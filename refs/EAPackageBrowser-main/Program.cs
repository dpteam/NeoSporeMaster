// ============================================================================
//  EA Package Browser (DBPF) — Complete Single-File Implementation v2.0
//  Target: .NET Framework 2.0 – 4.8
//  Namespace: EAPackageBrowser
//  No external libraries. No Resources. No App.config.
//  Supports: DBPF v1.0, v1.1, v2.0, v3.0 (Spore, SimCity, Sims 2/3/4)
//  Fault-tolerant: handles corrupted packages and versioning issues
//  Name resolution: NMAP, STBL, XML manifests, Spore locale
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ThreadingTimer = System.Threading.Timer;

namespace EAPackageBrowser
{
    // ========================================================================
    //  1. LOGGING (Trace → Console + File, coloured, datetime-stamped)
    // ========================================================================
    internal static class Log
    {
        private static readonly object _lock = new object();
        private static TextWriterTraceListener _fileListener;

        public static void Init()
        {
            Trace.AutoFlush = true;
            Trace.Listeners.Add(new ColoredConsoleListener());
            try
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir,
                    DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + ".log");
                _fileListener = new TextWriterTraceListener(logFile, "FileLog");
                Trace.Listeners.Add(_fileListener);
                Info("Log file: " + logFile);
            }
            catch (Exception ex)
            {
                Warn("Cannot create log file: " + ex.Message);
            }
        }

        public static void Shutdown()
        {
            try { Trace.Flush(); } catch { }
        }

        public static void Info(string msg) { Write("INFO", msg, ConsoleColor.Cyan); }
        public static void Ok(string msg) { Write(" OK ", msg, ConsoleColor.Green); }
        public static void Warn(string msg) { Write("WARN", msg, ConsoleColor.Yellow); }
        public static void Error(string msg) { Write(" ERR", msg, ConsoleColor.Red); }
        public static void Debug(string msg) { Write("DBG ", msg, ConsoleColor.Magenta); }
        public static void Status(string msg) { Write("STAT", msg, ConsoleColor.White); }

        private static void Write(string level, string msg, ConsoleColor color)
        {
            string line = string.Format("[{0}] [{1}] {2}",
                DateTime.Now.ToString("HH:mm:ss.fff"), level, msg);
            lock (_lock)
            {
                ConsoleColor old = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Trace.WriteLine(line);
                Console.ForegroundColor = old;
            }
        }
    }

    internal sealed class ColoredConsoleListener : TraceListener
    {
        public override void Write(string message) { Console.Write(message); }
        public override void WriteLine(string message) { Console.WriteLine(message); }
    }

    // ========================================================================
    //  2. DBPF CONSTANTS
    // ========================================================================
    internal static class DbpfConst
    {
        public const uint MAGIC_DBPF = 0x46504244; // "DBPF"
        public const uint MAGIC_DBPP = 0x50504244; // "DBPP" (encrypted)
        public const int HEADER_SIZE = 96;
        public const int IO_BUFFER_SIZE = 1024 * 1024; // 1 MB
        public const ushort COMPRESS_REFPACK = 0x10FB;
        public const ushort COMPRESS_REFPACK2 = 0xFB10;
        public const ushort COMPRESS_REFPACK3 = 0xFB50;
        public const ushort COMPRESS_STREAMABLE = 0xFFFE;
        public const ushort COMPRESS_ZLIB = 0x5A42;
        public const uint DIR_RESOURCE_TYPE = 0xE86B1EEF;
        public const uint NMAP_RESOURCE_TYPE = 0x0166038C;
        public const uint STBL_RESOURCE_TYPE = 0x220557DA;
        public const uint XML_MANIFEST_TYPE = 0x73E93EEB;
        public const uint LOCALE_TYPE_SPORE = 0x02FAC0B6;
        public const int CHECKPOINT_INTERVAL_MS = 600000; // 10 minutes
        public const int VIRTUAL_LIST_THRESHOLD = 5000;
    }

    // ========================================================================
    //  3. DBPF HEADER
    // ========================================================================
    internal sealed class DbpfHeader
    {
        public uint Magic;
        public uint MajorVersion;
        public uint MinorVersion;
        public uint UserMajorVersion;
        public uint UserMinorVersion;
        public uint Flags;
        public uint CreatedTimestamp;
        public uint ModifiedTimestamp;
        public uint IndexMajorVersion;
        public uint IndexEntryCount;
        public uint IndexFirstEntry;
        public uint IndexSize;
        public uint HoleEntryCount;
        public uint HoleOffset;
        public uint HoleSize;
        public uint IndexMinorVersion;
        public uint IndexOffset;
        public uint Unknown2;
        public byte[] Reserved;

        public bool IsEncrypted { get { return Magic == DbpfConst.MAGIC_DBPP; } }
        public bool IsVersion2 { get { return MajorVersion >= 2; } }
        public bool IsVersion3 { get { return MajorVersion >= 3; } }

        public static DbpfHeader Read(BinaryReader br)
        {
            DbpfHeader h = new DbpfHeader();
            h.Magic = br.ReadUInt32();
            h.MajorVersion = br.ReadUInt32();
            h.MinorVersion = br.ReadUInt32();
            h.UserMajorVersion = br.ReadUInt32();
            h.UserMinorVersion = br.ReadUInt32();
            h.Flags = br.ReadUInt32();
            h.CreatedTimestamp = br.ReadUInt32();
            h.ModifiedTimestamp = br.ReadUInt32();
            h.IndexMajorVersion = br.ReadUInt32();
            h.IndexEntryCount = br.ReadUInt32();
            h.IndexFirstEntry = br.ReadUInt32();
            h.IndexSize = br.ReadUInt32();
            h.HoleEntryCount = br.ReadUInt32();
            h.HoleOffset = br.ReadUInt32();
            h.HoleSize = br.ReadUInt32();
            h.IndexMinorVersion = br.ReadUInt32();
            h.IndexOffset = br.ReadUInt32();
            h.Unknown2 = br.ReadUInt32();
            h.Reserved = br.ReadBytes(24);
            return h;
        }

        public string GetVersionString()
        {
            return string.Format("{0}.{1}", MajorVersion, MinorVersion);
        }

        public string GetMagicString()
        {
            byte[] b = BitConverter.GetBytes(Magic);
            return Encoding.ASCII.GetString(b);
        }

        public string GetCreatedDate()
        {
            if (CreatedTimestamp == 0) return "N/A";
            try
            {
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                dt = dt.AddSeconds(CreatedTimestamp);
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch { return "0x" + CreatedTimestamp.ToString("X8"); }
        }

        public string GetModifiedDate()
        {
            if (ModifiedTimestamp == 0) return "N/A";
            try
            {
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                dt = dt.AddSeconds(ModifiedTimestamp);
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch { return "0x" + ModifiedTimestamp.ToString("X8"); }
        }
    }

    // ========================================================================
    //  4. DBPF INDEX ENTRY
    // ========================================================================
    internal sealed class DbpfEntry
    {
        public uint ResourceType;
        public uint ResourceGroup;
        public uint InstanceHi;
        public uint InstanceLo;
        public uint Offset;
        public uint FileSize;
        public uint MemSize;
        public ushort Compressed;
        public ushort Unknown;
        public bool IsDeleted;
        public int Index;
        public string ResolvedName; // resolved from NMAP / STBL / manifest

        public ulong Instance
        {
            get { return ((ulong)InstanceHi << 32) | (ulong)InstanceLo; }
            set { InstanceHi = (uint)(value >> 32); InstanceLo = (uint)(value & 0xFFFFFFFF); }
        }

        public uint PackedSize
        {
            get { return FileSize & 0x7FFFFFFF; }
        }

        public bool IsCompressed
        {
            get
            {
                return Compressed == 0xFFFF
                    || Compressed == DbpfConst.COMPRESS_REFPACK
                    || Compressed == DbpfConst.COMPRESS_REFPACK2
                    || Compressed == DbpfConst.COMPRESS_REFPACK3
                    || Compressed == DbpfConst.COMPRESS_STREAMABLE;
            }
        }

        public string GetTypeName()
        {
            return ResourceTypeNames.GetName(ResourceType);
        }

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(ResolvedName))
                return ResolvedName;
            return GetTypeName();
        }

        public string GetInstanceString()
        {
            return string.Format("0x{0:X8}:{1:X8}", InstanceHi, InstanceLo);
        }

        public string GetGroupString()
        {
            return string.Format("0x{0:X8}", ResourceGroup);
        }
    }

    // ========================================================================
    //  5. RESOURCE TYPE NAMES (Sims 2/3/4 + SimCity + Spore full list)
    // ========================================================================
    internal static class ResourceTypeNames
    {
        private static Dictionary<uint, string> _names;

        static ResourceTypeNames()
        {
            _names = new Dictionary<uint, string>();

            // ---- Sims 3 / Sims 4 common types ----
            _names[0x00AE6C67] = "BONE (skcon)";
            _names[0x00B2D882] = "_IMG (DDS)";
            _names[0x00B552EA] = "_SPT (SpeedTree)";
            _names[0x015A1849] = "GEOM (BodyGeometry)";
            _names[0x0166038C] = "NMAP (NameMap)";
            _names[0x01661233] = "MODL (ObjectModel)";
            _names[0x01A527DB] = "_AUD (voice/aud)";
            _names[0x01D0E6FB] = "VBUF (RCOL)";
            _names[0x01D0E70F] = "IBUF (RCOL)";
            _names[0x01D0E723] = "VRTF (RCOL)";
            _names[0x01D0E75D] = "MATD (Material)";
            _names[0x01D0E76B] = "SKIN (RCOL)";
            _names[0x01D10F34] = "MLOD (ModelLOD)";
            _names[0x01EEF63A] = "_AUD (fx/music)";
            _names[0x02019972] = "MTST (Scenegraph)";
            _names[0x021D7E8C] = "SPT2 (SpeedTree)";
            _names[0x0229684B] = "VBUF (RCOL2)";
            _names[0x0229684F] = "IBUF (RCOL2)";
            _names[0x022B756C] = "WorldDescNameHash";
            _names[0x025C90A6] = "_CSS";
            _names[0x025C95B6] = "LAYO (UILayout)";
            _names[0x025ED6F4] = "SIMO (SimOutfit)";
            _names[0x029E333B] = "VOCE (VoiceMix)";
            _names[0x02C9EFF2] = "MIXR (AudioSubMix)";
            _names[0x02D5DF13] = "JAZZ (AnimSeq)";
            _names[0x02DC343F] = "OBJK (ObjectInfo)";
            _names[0x033260E3] = "TKMK (TrackMask)";
            _names[0x0333406C] = "_XML";
            _names[0x033A1435] = "TXTC (TexCompositor)";
            _names[0x033B2B66] = "TXTC (World)";
            _names[0x0341ACC9] = "TXTF (Fabric)";
            _names[0x034AEECB] = "CASP (CASPart)";
            _names[0x0354796A] = "TONE (SkinTone)";
            _names[0x03555BA8] = "TONE (HairTone)";
            _names[0x0355E0A6] = "BOND (SlotAdjust)";
            _names[0x0358B08A] = "FACE (FaceBlend)";
            _names[0x03B33DDF] = "ITUN (InteractionTuning)";
            _names[0x03B4C61D] = "LITE (Light)";
            _names[0x03D843C2] = "CCHE (CacheEntry)";
            _names[0x03D86EA4] = "DETL (WorldLot)";
            _names[0x03E80CDC] = "HoodName";
            _names[0x0418FE2A] = "CFEN (Fence)";
            _names[0x044735DD] = "CFEN (World)";
            _names[0x044AE110] = "COMP (ComplatePreset)";
            _names[0x046A7235] = "LotLocationalData";
            _names[0x048A166D] = "UNKN (World)";
            _names[0x0498DA7E] = "GlobalLotID";
            _names[0x049CA4CD] = "CSTR (Stairs)";
            _names[0x04A09283] = "UNKN (LotStair)";
            _names[0x04A4D951] = "WDET (WorldDetail)";
            _names[0x04AC5D93] = "CPRX (ProxyProduct)";
            _names[0x04B30669] = "CTTL (TerrainBrush)";
            _names[0x04C58103] = "CRAL (Railing)";
            _names[0x04D82D90] = "CMRU (CacheMRU)";
            _names[0x04ED4BB2] = "CTPT (TerrainPaint)";
            _names[0x04EE6ABB] = "UNKN (TerrainTex)";
            _names[0x04F3CC01] = "CFIR (Fireplace)";
            _names[0x04F51033] = "SBNO (BinOutfit)";
            _names[0x04F66BCC] = "UNKN (FireplaceGrp)";
            _names[0x04F88964] = "SIME (SimExport)";
            _names[0x051DF2DD] = "CBLN (FacePiecePreset)";
            _names[0x05512255] = "UNKN (RoomIndex)";
            _names[0x0553EAD4] = "UNKN";
            _names[0x0563919E] = "UNKN (WallFloorComp)";
            _names[0x0580A2B4] = "THUM (png)";
            _names[0x0580A2B5] = "THUM (png)";
            _names[0x0580A2B6] = "THUM (png)";
            _names[0x0580A2CD] = "SNAP (png)";
            _names[0x0580A2CE] = "SNAP (sm)";
            _names[0x0580A2CF] = "SNAP (lg)";
            _names[0x0589DC44] = "THUM (png)";
            _names[0x0589DC45] = "THUM (png)";
            _names[0x0589DC46] = "THUM (png)";
            _names[0x0591B1AF] = "UPST (UserCAST)";
            _names[0x05B17698] = "THUM (png)";
            _names[0x05B17699] = "THUM (png)";
            _names[0x05B1769A] = "THUM (png)";
            _names[0x05B1B524] = "THUM (png)";
            _names[0x05B1B525] = "THUM (png)";
            _names[0x05B1B526] = "THUM (png)";
            _names[0x05CD4BB3] = "WorldRouting";
            _names[0x05DA8AF6] = "WBND (WorldBounds)";
            _names[0x05E4FAF7] = "UNKN";
            _names[0x05ED1226] = "REFS (References)";
            _names[0x05FF3549] = "UNKN";
            _names[0x05FF6BA4] = "2ARY (LotGeo)";
            _names[0x0604ABDA] = "DMTR (DreamTree)";
            _names[0x060B390C] = "CWAT (WaterBrush)";
            _names[0x060E1826] = "UNKN";
            _names[0x0611B0E7] = "UNKN";
            _names[0x062853A8] = "FAMD (Household)";
            _names[0x062C8204] = "BBLN (ClothingParts)";
            _names[0x062E9EE0] = "BBLN (UI)";
            _names[0x06302271] = "CINF (ColorInfo)";
            _names[0x063261DA] = "HINF (HairColor)";
            _names[0x06326213] = "OBCI (ObjColor)";
            _names[0x06393F5D] = "UNKN";
            _names[0x065B8B38] = "UNKN";
            _names[0x065BFCAC] = "UNKN (World)";
            _names[0x065BFCAD] = "UNKN (World)";
            _names[0x065BFCAE] = "UNKN (World)";
            _names[0x0668F628] = "WorldDescDetails";
            _names[0x0668F630] = "WorldDescLotCount";
            _names[0x0668F635] = "TWNI (WorldImg)";
            _names[0x0668F639] = "TWNP (WorldImgPath)";
            _names[0x067CAA11] = "BGEO (BlendGeom)";
            _names[0x06B981ED] = "OBJS (ScriptState)";
            _names[0x06CE4804] = "META (MetaData)";
            _names[0x06D6B112] = "UNKN";
            _names[0x06DC847E] = "UNKN (LotTerrainPaint)";
            _names[0x073FAA07] = "S3SA (Script)";
            _names[0x07046B39] = "InstalledSimsIndex";
            _names[0x07CD07EC] = "FrontDoorRef";
            _names[0x0A36F07A] = "CCFP (Fountain)";
            _names[0x0C37A5B5] = "LOOK (LookupTable)";
            _names[0x0C07456D] = "COLL (Collection)";
            _names[0x11E32896] = "RoofDefs";
            _names[0x16B17A6C] = "ThumbExpiration";
            _names[0x1F886EAD] = "_INI";
            _names[0x220557DA] = "STBL (StringTable)";
            _names[0x2AD195F2] = "STBL (World)";
            _names[0x2653E3C8] = "THUM (png)";
            _names[0x2653E3C9] = "THUM (png)";
            _names[0x2653E3CA] = "THUM (png)";
            _names[0x2D4284F0] = "THUM (png)";
            _names[0x2D4284F1] = "THUM (png)";
            _names[0x2D4284F2] = "THUM (png)";
            _names[0x2E75C764] = "ICON (sm)";
            _names[0x2E75C765] = "ICON (md)";
            _names[0x2E75C766] = "ICON (lg)";
            _names[0x2E75C767] = "ICON (xl)";
            _names[0x2F7D0002] = "IMAG (TGA)";
            _names[0x2F7D0004] = "IMAG (PNG)";
            _names[0x2F7D0008] = "UITX (UVMap)";
            _names[0x312E7545] = "UNKN (Walls)";
            _names[0x316C78F2] = "CFND (Foundation)";
            _names[0x319E4F1D] = "OBJD (ObjectDef)";
            _names[0x32C83095] = "ThumbValidation";
            _names[0x342778A7] = "FaceOverlays";
            _names[0x342779A7] = "FaceOverlays (World)";
            _names[0x34E5247C] = "UNKN";
            _names[0x35A33E29] = "WorldDescHash";
            _names[0x3A65AF29] = "MINF (Makeup)";
            _names[0x3D8632D0] = "WorldTerrainPaint";
            _names[0x4D1A5589] = "OBJN (ObjectInst)";
            _names[0x4F09F8E1] = "UNKN";
            _names[0x515CA4CD] = "CWAL (Wall/Floor)";
            _names[0x54372472] = "TSNP (TravelSnap)";
            _names[0x5DE9DBA0] = "THUM (png)";
            _names[0x5DE9DBA1] = "THUM (png)";
            _names[0x5DE9DBA2] = "THUM (png)";
            _names[0x626F60CC] = "THUM (CAS)";
            _names[0x626F60CD] = "THUM (CAS)";
            _names[0x626F60CE] = "THUM (CAS)";
            _names[0x628A788F] = "NeighborhoodInfo";
            _names[0x63A33EA7] = "ANIM (Scenegraph)";
            _names[0x6ABFAD26] = "UNKN";
            _names[0x6B20C4F3] = "CLIP (Animation)";
            _names[0x6B6D837D] = "SNAP (famSm)";
            _names[0x6B6D837E] = "SNAP (famMd)";
            _names[0x6B6D837F] = "SNAP (famLg)";
            _names[0x72683C15] = "STPR (SkinTonePreset)";
            _names[0x736884F1] = "VPXY (VisualProxy)";
            _names[0x73E93EEB] = "_XML (Manifest)";
            _names[0x7672F0C5] = "PackageDeps";
            _names[0x8070223D] = "AUDT (AudioTuner)";
            _names[0x82B43584] = "Checksum";
            _names[0x8EAF13DE] = "_RIG (GrannyRig)";
            _names[0x8FFB80F6] = "_ADS (DDS)";
            _names[0x90620000] = "UNKN (World)";
            _names[0x90624C1B] = "UNKN (World)";
            _names[0x9063660D] = "WTXT (WorldTexMap)";
            _names[0x9063660E] = "RoadsIntersection";
            _names[0x913381F2] = "UNKN";
            _names[0x9151E6BC] = "CWST (WallStyle)";
            _names[0x91EDBD3E] = "CRST (RoofStyle)";
            _names[0x94C5D14A] = "SIGR (Signature)";
            _names[0x94EC4B54] = "UNKN";
            _names[0xA2377025] = "UNKN (Save)";
            _names[0xA5F9FE18] = "UNKN (SocialRouting)";
            _names[0xA8D58BE5] = "SKIL (SkillData)";
            _names[0xAE39399F] = "WorldGeometry";
            _names[0xB074ACE6] = "WorldGeometry2";
            _names[0xB125533A] = "UNKN";
            _names[0xB1422971] = "UNKN";
            _names[0xB1CC1AF6] = "_VID (GSTRMovie)";
            _names[0xB4DD716B] = "_INV (Inventory)";
            _names[0xB52F5055] = "FBLN (BlendUnit)";
            _names[0xCF84EC98] = "StateMachineState";
            _names[0xCF9A4ACE] = "MDLR (Modular)";
            _names[0xD063545B] = "LDES (LotDesc)";
            _names[0xD3044521] = "RSLT (SlotDef)";
            _names[0xD382BF57] = "FTPT (Footprint)";
            _names[0xD4D9FBE5] = "PTRN (PatternList)";
            _names[0xD84E7FC5] = "ICON (lotSm)";
            _names[0xD84E7FC6] = "ICON (lotMd)";
            _names[0xD84E7FC7] = "ICON (lotLg)";
            _names[0xD9BD0909] = "WorldDescName";
            _names[0xDC37E964] = "TextResource";
            _names[0xDD3223A7] = "BUFF (BuffData)";
            _names[0xDEA2951C] = "PETB (PetBreed)";
            _names[0xE86B1EEF] = "DIR";
            _names[0xEA5118B0] = "_SWB (EffectTree)";
            _names[0xF0633989] = "UNKN (World)";
            _names[0xF0FF5598] = "CAM (CameraPreset)";
            _names[0xF12E5E12] = "UNKN";
            _names[0xF1EDBD86] = "CRMT (RoofPattern)";
            _names[0xF3A38370] = "NGMP (NameHashMap)";
            _names[0xF609FD60] = "WorldPackage";
            _names[0xFCEAB65B] = "THUM (ColourSwatch)";

            // ---- Spore types (from reg_type.txt) ----
            _names[0x00B1B104] = "prop";
            _names[0x02B9F662] = "soundProp/audioProp";
            _names[0x00E6BCE5] = "gmdl";
            _names[0x011989B7] = "plt";
            _names[0x01AD2416] = "creature_traits";
            _names[0x01AD2417] = "building_traits";
            _names[0x01AD2418] = "vehicle_traits";
            _names[0x01C135DA] = "gmsh";
            _names[0x01C3C4B3] = "trait_pill";
            _names[0x0248F226] = "css (Spore)";
            _names[0x024A0E52] = "trigger";
            _names[0x02523258] = "formation/arg";
            _names[0x276CA4B9] = "ttf";
            _names[0x027C5CEF] = "ttf (alt)";
            _names[0x02D5C9AF] = "summary";
            _names[0x02D5C9B0] = "summary_pill";
            _names[0x02FAC0B6] = "locale";
            _names[0x030BDEE3] = "pollen_metadata";
            _names[0x0376C3DA] = "hm";
            _names[0x0472329B] = "htra/arth";
            _names[0x065266B7] = "xhtml";
            _names[0x2F4E681C] = "raster";
            _names[0x3F8662EA] = "jpg";
            _names[0x2F7D0002] = "jpeg";
            _names[0x2F7D0005] = "bmp";
            _names[0x2F7D0006] = "tga";
            _names[0x2F7D0007] = "gif";
            _names[0x14885D1C] = "ico";
            _names[0x4AEB6BC6] = "tlsa";
            _names[0x7C19AA7A] = "pctp";
            _names[0xCF6C21B8] = "xml (Spore)";
            _names[0xC7A91467] = "layout";
            _names[0xEFBDA3FF] = "layout (alt)";
            _names[0x050F4731] = "layout_bin";
            _names[0x12952634] = "dat";
            _names[0x1A99B06B] = "bem";
            _names[0x1E99B626] = "bat";
            _names[0x1F639D98] = "xls";
            _names[0x2399BE55] = "bld";
            _names[0x24682294] = "vcl";
            _names[0x250FE9A2] = "spui";
            _names[0x25DF0112] = "gait";
            _names[0x2B6CAB5F] = "txt";
            _names[0x2B978C46] = "crt";
            _names[0x376840D7] = "vp6";
            _names[0x37979F71] = "cfg";
            _names[0x3C77532E] = "psd";
            _names[0x3C7E0F63] = "mcl";
            _names[0x3D97A8E4] = "cll";
            _names[0x3F9C28B5] = "ani";
            _names[0x438F6347] = "flr";
            _names[0x448AE7E2] = "hkx";
            _names[0x476A98C7] = "ufo";
            _names[0x497767B9] = "pfx";
            _names[0x5C74D18B] = "density";
            _names[0x617715C4] = "py";
            _names[0x617715D9] = "pd";
            _names[0x9B8E862F] = "world (Spore)";
            _names[0xDFAD9F51] = "cell";
            _names[0xEE17C6AD] = "animation";
            _names[0x055ADA24] = "cnv";
            _names[0x2F4E681B] = "rw4";
            _names[0x76E1259D] = "physx_bin";
            _names[0x7EAB18FD] = "physx_xml";
            _names[0x278CF8F2] = "cfx/gfx";
            _names[0x17952E6C] = "dds (Spore)";
            _names[0x01A527DB] = "snr";
            _names[0x01EEF63A] = "sns";
            _names[0x0469A3F7] = "smt";
            _names[0x022D2C83] = "pdr";
            _names[0x06EFC6AA] = "package (Spore)";
            _names[0x03055F61] = "instrument";
            _names[0x0497925E] = "mode";
            _names[0x03F51892] = "children";
            _names[0x042C9CBB] = "snd";
            _names[0x02C9EFF2] = "submix";
            _names[0x029E333B] = "voice";
            _names[0x047B8300] = "lvl/scn";
            _names[0x4B9EF6DC] = "structure";
            _names[0xDA141C1B] = "populate";
            _names[0x04805684] = "advect";
            _names[0xD92AF091] = "loottable";
            _names[0xDBA35AE2] = "look_algorithm";
            _names[0x612B3191] = "backgroundMap";
            _names[0xF9C3D770] = "random_creature";
            _names[0x8C042499] = "look_table";
            _names[0x433FB70C] = "effectMap";
            _names[0x2A3CE5B7] = "globals";
            _names[0x0F43029A] = "creaturedata";
            _names[0x76A8F7D8] = "geo/Noun.bin";
            _names[0x02393756] = "cur";
            _names[0xEA5118B0] = "effdir";
            _names[0xDD6233D6] = "html";
            _names[0x1E639C34] = "xml (alt)";
            _names[0x00339479] = "animstate";
            _names[0x72047DE2] = "bmdl";
            _names[0x3681D755] = "lua";
            _names[0x03E421E9] = "bitImage";
            _names[0x03E421EC] = "8bitImage";
            _names[0x03E421F0] = "16bitImage";
            _names[0x03E421ED] = "32bitImage";
            _names[0x03E421EF] = "48bitImage";
            _names[0x5CAA5E28] = "blockdata";
            _names[0x6D7728C3] = "db";
            _names[0xE96B3B72] = "blend";
            _names[0xE5CE9447] = "blend1";
            _names[0x59770944] = "XA";
            _names[0x3B72D4F1] = "rdx";
            _names[0x86C5375A] = "rdx9";
            _names[0x02CB4F2F] = "pcaw";
            _names[0x477764F5] = "pdn";
            _names[0x2081B4F5] = "log";
            _names[0x1E99B639] = "bak";
            _names[0x2699C284] = "bin";
            _names[0x1999AE0B] = "bfx";
            _names[0x04F684A4] = "cmp";
            _names[0x67771F5C] = "js";
            _names[0xB8444447] = "png.dds";
            _names[0xE1E7F839] = "pollen_metadata.bak";
            _names[0x041E19BE] = "avi";
            _names[0xEF7D16E1] = "uiTexture";
            _names[0x02C23DF7] = "activeLog";
            _names[0x20426A63] = "crystalTuning";
            _names[0xE5C2D838] = "eliteNPCGlobals";
            _names[0x7CA6C6C9] = "npcAffix";
            _names[0x36201B51] = "magicNumbers";
            _names[0x92EA4AAC] = "serverEventDef";
            _names[0x8E94F44C] = "difficultyTuning";
            _names[0xA8A25294] = "chainLevels";
            _names[0xA38FA119] = "sectionConfig";
            _names[0x9342C4D3] = "directorTuning";
            _names[0xAAED883B] = "characterType";
            _names[0x61BF29AA] = "lootPreferences";
            _names[0x58E9A177] = "weaponTuning";
            _names[0x534136B6] = "unlocksTuning";
            _names[0x60C1B295] = "spaceshipTuning";
            _names[0x17BBCE29] = "characterAnimation";
            _names[0xE51118C3] = "prop_new";
            _names[0x2AE9952D] = "obstacle_info";
            _names[0xA11D3144] = "markerSet";
            _names[0x9F8087D5] = "condition";
            _names[0x30728CE7] = "phase";
            _names[0xEEEB0E31] = "aiDefinition";
            _names[0x474940A5] = "ClassAttributes";
            _names[0xD117AFCA] = "NonPlayerClass";
            _names[0x7060F453] = "mounted";
            _names[0xBCC56192] = "direct";
            _names[0x5F0A4B8A] = "detail";
            _names[0x543C4F0F] = "round";
            _names[0x6C619018] = "square";
            _names[0xA659762B] = "origin";
            _names[0xB275D7E9] = "png.png";
            _names[0x0A4D8D09] = "bnk";
            _names[0x0A98EAF0] = "json";
            _names[0x08068AEC] = "er2";
            _names[0x0330A65E] = "eapdPixie";
            _names[0x2B7DF427] = "mrk";
            _names[0x01005967] = "aif";
            _names[0xF2B924B3] = "ltp";
            _names[0x017F243B] = "kGameData/cGameData";
            _names[0x02A37E35] = "kCheatObject";
            _names[0x02AFD284] = "kMissionFetch";
            _names[0x04F3BDE9] = "kMissionHappinessEvent";
            _names[0x04F77499] = "kMissionMakeAlly";
            _names[0x0397BFF3] = "kRaidPlunderEvent";
            _names[0x03960C0E] = "kRaidEvent";
            _names[0x02BA2A0E] = "kMissionColonize";
            _names[0x02F99994] = "kMissionEradicate";
            _names[0x032A12F1] = "kMissionExplore";
            _names[0x0330FC49] = "kMissionWar";
            _names[0x0347092D] = "kMissionScan";
            _names[0x035988F4] = "kMissionTerraform";
            _names[0x02ADEDFF] = "kMissionTrade";
            _names[0x0447092D] = "kMissionTradeRoute";
            _names[0x035ED8F6] = "kMissionMultiDelivery";
            _names[0x0437444B] = "kMissionBiosphere";
            _names[0x0317AFCC] = "kGenericPressureEvent";
            _names[0x04222283] = "kMissionMultiStep";
            _names[0x0347092E] = "kMissionBalance";
            _names[0x0347092F] = "kMissionFindAliens";
            _names[0x04222284] = "kMissionFlight101";
            _names[0x02ADEDEE] = "kMissionUseTool";
            _names[0x04222285] = "kMissionStory201";
            _names[0x04622285] = "kMissionChangeArchetype";
            _names[0x04633285] = "kMissionTrackBadge";
            _names[0x046332A5] = "kMissionAdventure";
            _names[0x02C21781] = "kPlayer/cPlayer";
            _names[0x02E9AE6C] = "kMilitaryAttackCityOrder";
            _names[0x03DF2CC5] = "kCulturalConvertCityOrder";
            _names[0x018C40BC] = "kGameTerrainCursor";
            _names[0x018EA1EB] = "kBuildingCityHall";
            _names[0x018EA2CC] = "kBuildingIndustry";
            _names[0x018EB106] = "kBuildingHouse";
            _names[0x01A56ABA] = "kBuildingEntertainment";
            _names[0x070703B3] = "kBuildingScenario";
            _names[0x0436F342] = "kTurret";
            _names[0x038CFB6B] = "kCelestialBody";
            _names[0x018C431C] = "kGameBundle";
            _names[0x03D5C325] = "kCulturalTarget";
            _names[0x018C43E8] = "kCity";
            _names[0x01906183] = "kGameBundleGroundContainer";
            _names[0x018C6DE8] = "kVehicle";
            _names[0x02E98AB7] = "kVehicleGroupOrder";
            _names[0x018C7C97] = "kCityWalls";
            _names[0x0244FB08] = "kCityTerritory";
            _names[0x02C5C93A] = "kPlaceholderColonyEditorCursor";
            _names[0x02DD8C42] = "kPlanetaryArtifact";
            _names[0x04E3FAB5] = "kToolObject";
            _names[0x018C816A] = "kCivilization";
            _names[0x018EB45E] = "kCreatureAnimal";
            _names[0x018EB4B7] = "kCreatureCitizen";
            _names[0x03ED8573] = "kObstacle";
            _names[0x018C84A9] = "kGamePlant";
            _names[0x02C9CC91] = "kFruit";
            _names[0x02E96892] = "kFruitGroup";
            _names[0x52AA6122] = "kNest";
            _names[0x01BE418E] = "kHerd";
            _names[0x02A034CD] = "kEgg";
            _names[0x03A2511E] = "kInteractiveOrnament";
            _names[0x036BE27E] = "kGameplayMarker";
            _names[0x018C88E4] = "kOrnament";
            _names[0x061494BE] = "kAnimalTrap";
            _names[0x055CF865] = "kTotemPole";
            _names[0x0629BAFE] = "kTribeFoodMat";
            _names[0x02A8FB3F] = "kRock";
            _names[0x0403DF5C] = "kCommodityNode";
            _names[0x0283DDB1] = "kMovableDestructibleOrnament";
            _names[0x032F9778] = "kSolarHitSphere";
            _names[0x02E72CAE] = "kHitSphere";
            _names[0x02B8A4E7] = "kInterCityRoad";
            _names[0x018C6D19] = "kTribe";
            _names[0x018C8F0C] = "kTribeTool";
            _names[0x024270C9] = "kSpear";
            _names[0x018C9380] = "kArtilleryProjectile";
            _names[0x0240E3BF] = "kFlakProjectile";
            _names[0x024270C5] = "kDefaultToolProjectile";
            _names[0x024270C7] = "kDeepSpaceProjectile";
            _names[0x0244D3C8] = "kSpaceDefenseMissile";
            _names[0x024630D7] = "kDefaultBeamProjectile";
            _names[0x04167186] = "kDefaultAoEArea";
            _names[0x04F76F0D] = "kCulturalProjectile";
            _names[0x05776A2C] = "kResourceProjectile";
            _names[0x049CEC61] = "kICBM";
            _names[0x018EB641] = "kSoundLoopObject";
            _names[0x02EE8CE8] = "kRotationRing";
            _names[0x07292112] = "kRotationBall";
            _names[0x07A30A12] = "kMorphHandle";
            _names[0x076F6E64] = "kTargetMorphHandle";
            _names[0x0771AD6F] = "kArrowMorphHandle";
            _names[0x07A81829] = "kSimpleRotationRing";
            _names[0x07ABDD91] = "kSimpleRotationBall";
            _names[0x03275728] = "kPlanet";
            _names[0x044462A6] = "kVisiblePlanet";
            _names[0x0355C93A] = "kStar";
            _names[0x038CF94C] = "kSolarSystem";
            _names[0x03572E72] = "kSimPlanetLowLOD";
            _names[0x018EB9D2] = "kEmpire";
            _names[0x021FFA3F] = "kSpaceInventory";
            _names[0x02265FDC] = "kPlayerInventory";
            _names[0x018EBADC] = "kGameDataUFO";
            _names[0x01E4DAAE] = "kTribeHut";
            _names[0x3098AF98] = "kTribePlanner";
            _names[0x1406A572] = "kCreatureSpeciesMission";
            _names[0x34066DBA] = "kCreatureTutorialMission";
            _names[0x5436411A] = "kTribeToTribeMission";
            _names[0x14D9597F] = "kFindMission";
            _names[0x06EF0F11] = "kNanoDrone";
            _names[0x074E0069] = "kPlaceableSound";
            _names[0x07B38BA7] = "kPlaceableEffect";
            _names[0x05220CB8] = "cPlanetRecord";
            _names[0x021F10B2] = "cTimeOfDay";
            _names[0x021F1105] = "cNpcTribeController";
            _names[0x021F1187] = "cCitySimulator";
            _names[0x023BA44D] = "cCombatSimulator";
            _names[0x021F11AE] = "cPosseSimulator";
            _names[0x021F15DD] = "cSimulatorSpaceGame";
            _names[0x021F11C4] = "cSimPlanetHighLOD";
            _names[0x02E1923C] = "cVehicleTicker";
            _names[0x02CB0EE3] = "cBadgeManager";
            _names[0x02AA56D0] = "cMissionManager";
            _names[0xD406729F] = "cCreatureMissionManager";
            _names[0x74336F3E] = "cTribeMissionManager";
            _names[0x058B3257] = "cCivMissionManager";
            _names[0x02A8BFBE] = "cSimulatorUniverse";
            _names[0x02F011ED] = "cSimulatorPlayerUFO";
            _names[0x3226D7D4] = "cDramaManager";
            _names[0x042232A1] = "cPlayerCityMusic";
            _names[0x0424EA07] = "cNPCCityMusic";
            _names[0x04238ECC] = "cEmpireGrowthSim";
            _names[0x018EB5C7] = "cRelationshipManager";
            _names[0x03A3AC9F] = "cCollectableItems";
            _names[0x037ABB4A] = "cCommEvent";
            _names[0x0447341A] = "cEventLogCommItem";
            _names[0x032229FF] = "cSpaceToolData";
            _names[0x0223BE7B] = "cSpaceInventoryItem";
            _names[0x0384F82A] = "cAnimalCargoInfo";
            _names[0x0384EF2A] = "cPlantCargoInfo";
            _names[0x03E4353C] = "cStarRecord";
            _names[0x03E434E2] = "cPlanetRecord (alt)";
            _names[0x02F404EA] = "cSimulatorWalkAround";

            // Spore creature part types
            _names[0x83324B49] = "cilia";
            _names[0x77FBB925] = "flagella";
            _names[0x0DAE69D1] = "chemical";
            _names[0x77B380C3] = "filter";
            _names[0x2E8647BD] = "jaw";
            _names[0x6ED040F4] = "poker";
            _names[0xC1B86F04] = "electric";
            _names[0x002AE8E9] = "spike";
            _names[0x2A86410A] = "jet";
            _names[0x81B07013] = "skinned";
            _names[0x067489DC] = "xaxis";
            _names[0x3BC16BCD] = "yaxis";
            _names[0x01D369EE] = "zaxis";
            _names[0xAEB95D5B] = "flag";
            _names[0x45E82E50] = "gear";
            _names[0x735CF023] = "light (Spore)";
            _names[0x7BD05F63] = "roof";
            _names[0x76822486] = "center";
            _names[0x26FEA3BE] = "sign";
            _names[0x7DB16DB9] = "wall";
            _names[0x22CAC325] = "windmill";
            _names[0x28016132] = "rounddormer";
            _names[0xA28487B6] = "roundskylight";
            _names[0x55ACCFF5] = "squareskylight";
            _names[0x69F9E3F6] = "ball";
            _names[0x4A66DE36] = "stalk";
            _names[0x5CD23F68] = "omnivore";
            _names[0xA28A67E8] = "carnivore";
            _names[0x2DFB4F9F] = "herbivore";
            _names[0x3E9C273B] = "air";
            _names[0x28346586] = "land";
            _names[0xF83E63C9] = "space";
            _names[0x9E3C3DFA] = "water";
            _names[0x99C183C7] = "rear";
            _names[0x29FEA8EC] = "side";
            _names[0xAC6EA6B2] = "wheel";
            _names[0x299BC2F9] = "fixed";
            _names[0x54D42F94] = "flapping";
            _names[0x754BE343] = "powers";
            _names[0x3E6CC964] = "tmp";
            _names[0x17F3ADEF] = "reversed";
            _names[0x2E5A9763] = "LRUMetaData";

            // Spore group IDs used as type hints
            _names[0x07720FE1] = "AccessoryCivCreatures";
            _names[0x5D0943A9] = "AccessorySpaceCreatures";
            _names[0x8D6D40DF] = "AccessoryTribeCreatures";
            _names[0x484FF0B8] = "Buildings";
            _names[0xA426730B] = "CellGame";
            _names[0x115FA309] = "CityHall";
            _names[0xBEB528CB] = "CivGame";
            _names[0xD2E52E5A] = "CLG_StarterCells";
            _names[0xF6EF5A49] = "ColonyAir";
            _names[0x95693A90] = "ColonyLand";
            _names[0xD6CC95F8] = "ColonySea";
            _names[0x18CE4C0E] = "CreatureAnimal";
            _names[0xCA5828CC] = "CreatureCiv";
            _names[0xAD56080C] = "CreatureGame";
            _names[0xD49DE19D] = "Creatures";
            _names[0xD74E4828] = "CreatureSpace";
            _names[0x2E9772DA] = "CreatureTribe";
            _names[0x67D5DAF9] = "CRG_StarterCreatures";
            _names[0xAD76C2CD] = "CulturalAir";
            _names[0x0222DED4] = "CulturalLand";
            _names[0xA5A3D5A4] = "CulturalSea";
            _names[0x85E38D82] = "EconomicAir";
            _names[0x13D90629] = "EconomicLand";
            _names[0x6605EDB3] = "EconomicSea";
            _names[0xD59E9335] = "Editable";
            _names[0x36F60725] = "Entertainment";
            _names[0x51FBB249] = "Factory";
            _names[0x5BDCEC35] = "Flora";
            _names[0xE6BDCC2E] = "FloraLarge";
            _names[0x3FA39F40] = "FloraMedium";
            _names[0xD318DDCA] = "FloraSmall";
            _names[0xBDA06A7C] = "Home";
            _names[0xDDC202B3] = "House";
            _names[0xC3FDE0B5] = "LoadableCivCreatures";
            _names[0x838C8F5D] = "LoadableSpaceCreatures";
            _names[0x14F090CB] = "LoadableTribeCreatures";
            _names[0xFA167DA8] = "MilitaryAir";
            _names[0xDF036863] = "MilitaryLand";
            _names[0xDA41D879] = "MilitarySea";
            _names[0xEDF036D6] = "Music";
            _names[0xB09ABBDA] = "Planets";
            _names[0x72E47835] = "Scans";
            _names[0x2DB6DAD3] = "SpaceGame";
            _names[0x996D126C] = "TRG_StarterCreatures";
            _names[0xF71FA311] = "TribeGame";
            _names[0x37216F9C] = "UserCreated";
            _names[0x89CA8A12] = "UserPollinated";
            _names[0x01888B0D] = "ValidCivCreatures";
            _names[0x11886885] = "ValidSpaceCreatures";
            _names[0xED942043] = "ValidTribeCreatures";
            _names[0x39E64A16] = "Vehicles";
            _names[0x8DF88930] = "CreatureAdventurer";

            // Spore fixed object types
            _names[0x0FFC2CEE] = "FixedObjectAudio";
            _names[0x14E37D55] = "FixedObjectAudioCrowd";
            _names[0x422CB40D] = "FixedObjectAudioEnvironment";
            _names[0x16C10BFF] = "FixedObjectAudioMechanical";
            _names[0x96B21AF3] = "FixedObjectAudioMusic";
            _names[0x59FB17B2] = "FixedObjectAudioNote";
            _names[0x882BAD3E] = "FixedObjectAudioOneshot";
            _names[0x1CCCD45B] = "FixedObjectEffect";
            _names[0x15553342] = "FixedObjectEffectContinuous";
            _names[0xAFECBB58] = "FixedObjectEffectLights";
            _names[0xDACEDE62] = "FixedObjectEffectNature";
            _names[0xC68095C9] = "FixedObjectEffectOneshot";
            _names[0xDF3A3835] = "FixedObjectEffectSciFi";
            _names[0xCE3A2E8C] = "FixedObjectEffectWhimsical";
            _names[0xF25F3E76] = "FixedObjectFlora";
            _names[0x015677BF] = "FixedObjectFloraLarge";
            _names[0x28C176EB] = "FixedObjectFloraMedium";
            _names[0xDB5CAD8B] = "FixedObjectFloraSmall";
            _names[0x41B55D34] = "FixedObjectGameplay";
            _names[0x27100213] = "FixedObjectRock";
            _names[0xA20B0DB0] = "FixedObjectCanBeHeld";
            _names[0x61265923] = "FixedObjects";
            _names[0xE782B1B7] = "FixedObjectStaticObject";
            _names[0xD2050CB7] = "LoadableAdventurerCreatures";

            // Spore scenario types
            _names[0x287ADCDC] = "ScenarioGenreAttack";
            _names[0x25A6EA6E] = "ScenarioGenreCollect";
            _names[0xC34C5E14] = "ScenarioGenreDefend";
            _names[0x37FD4E0D] = "ScenarioGenreExplore";
            _names[0xE27DDAD4] = "ScenarioGenrePuzzle";
            _names[0xC422519E] = "ScenarioGenreQuest";
            _names[0xFB734CD1] = "ScenarioGenreSocialize";
            _names[0xB4707F8F] = "ScenarioGenreStory";
            _names[0x27818FE6] = "ScenarioGenreTemplate";
            _names[0x24720859] = "ScenarioPlanet";
            _names[0x14E497ED] = "ScenarioPlayable";
            _names[0x8D780FDE] = "Scenarios";
            _names[0x20790816] = "ScenarioUnset";
            _names[0x0647493B] = "ScenarioUsable";
            _names[0x5144C45F] = "ValidAdventurerCreatures";

            // Spore adventure / darkspore types
            _names[0x1BCED3D7] = "LootRigblock";
            _names[0x1DFEA336] = "TestAsset";
            _names[0x5CB9F7C7] = "AffixTuning";
            _names[0x6A1812C6] = "lootprefix";
            _names[0x23EF98FD] = "PVPLevels";
            _names[0x52D095F6] = "LevelConfig";
            _names[0x3583564E] = "ObjectExtents";
            _names[0x68232164] = "LevelObjectives";
            _names[0xB9193960] = "Level";
            _names[0xBBA5400D] = "NavPowerTuning";
            _names[0x447DC2E5] = "LootSuffix";
            _names[0xDEAF6ADE] = "bak.png";
            _names[0xDC74130E] = "bak2.png";
            _names[0x793F6424] = "blockinfo";
            _names[0x366A930D] = "adv";
        }

        public static string GetName(uint typeId)
        {
            string name;
            if (_names.TryGetValue(typeId, out name))
                return name;
            return string.Format("0x{0:X8}", typeId);
        }

        public static bool IsKnown(uint typeId)
        {
            return _names.ContainsKey(typeId);
        }
    }

    // ========================================================================
    //  6. NAME RESOLVER (NMAP, STBL, XML Manifest parsing)
    // ========================================================================
    internal static class NameResolver
    {
        /// <summary>
        /// Attempts to resolve resource names from NMAP, STBL, and XML manifest
        /// resources found within the package.
        /// </summary>
        public static void ResolveNames(DbpfDecoder decoder)
        {
            if (decoder == null || decoder.Entries == null) return;

            // Build lookup: instance -> entry for quick matching
            Dictionary<ulong, DbpfEntry> instanceMap = new Dictionary<ulong, DbpfEntry>();
            for (int i = 0; i < decoder.Entries.Count; i++)
            {
                DbpfEntry e = decoder.Entries[i];
                ulong key = e.Instance;
                if (!instanceMap.ContainsKey(key))
                    instanceMap[key] = e;
            }

            // 1) Parse NMAP resources (Sims 3 Name Map)
            for (int i = 0; i < decoder.Entries.Count; i++)
            {
                DbpfEntry e = decoder.Entries[i];
                if (e.ResourceType == DbpfConst.NMAP_RESOURCE_TYPE)
                {
                    try
                    {
                        byte[] data = decoder.ReadEntryData(e);
                        if (data != null && data.Length > 8)
                            ParseNMAP(data, instanceMap);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("NMAP parse error: " + ex.Message);
                    }
                }
            }

            // 2) Parse STBL resources (String Tables)
            for (int i = 0; i < decoder.Entries.Count; i++)
            {
                DbpfEntry e = decoder.Entries[i];
                if (e.ResourceType == DbpfConst.STBL_RESOURCE_TYPE)
                {
                    try
                    {
                        byte[] data = decoder.ReadEntryData(e);
                        if (data != null && data.Length > 4)
                            ParseSTBL(data, e, instanceMap);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("STBL parse error: " + ex.Message);
                    }
                }
            }

            // 3) Parse XML Manifest (Sims 3 package manifest)
            for (int i = 0; i < decoder.Entries.Count; i++)
            {
                DbpfEntry e = decoder.Entries[i];
                if (e.ResourceType == DbpfConst.XML_MANIFEST_TYPE)
                {
                    try
                    {
                        byte[] data = decoder.ReadEntryData(e);
                        if (data != null && data.Length > 10)
                            ParseXMLManifest(data, instanceMap);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("XML manifest parse error: " + ex.Message);
                    }
                }
            }

            // 4) Parse Spore locale resources
            for (int i = 0; i < decoder.Entries.Count; i++)
            {
                DbpfEntry e = decoder.Entries[i];
                if (e.ResourceType == DbpfConst.LOCALE_TYPE_SPORE)
                {
                    try
                    {
                        byte[] data = decoder.ReadEntryData(e);
                        if (data != null && data.Length > 8)
                            ParseSporeLocale(data, e, instanceMap);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("Spore locale parse error: " + ex.Message);
                    }
                }
            }

            int resolved = 0;
            for (int i = 0; i < decoder.Entries.Count; i++)
            {
                if (!string.IsNullOrEmpty(decoder.Entries[i].ResolvedName))
                    resolved++;
            }
            if (resolved > 0)
                Log.Ok("Resolved " + resolved + " resource name(s) from internal data.");
        }

        /// <summary>
        /// NMAP format (Sims 3):
        ///   uint32 count
        ///   For each entry:
        ///     uint32 instanceHi
        ///     uint32 instanceLo
        ///     uint16 nameLength
        ///     byte[nameLength] name (UTF-8)
        /// </summary>
        private static void ParseNMAP(byte[] data, Dictionary<ulong, DbpfEntry> map)
        {
            if (data.Length < 4) return;
            int pos = 0;
            uint count = ReadU32(data, ref pos);
            if (count > 100000) return; // sanity

            for (uint i = 0; i < count && pos + 10 <= data.Length; i++)
            {
                uint instHi = ReadU32(data, ref pos);
                uint instLo = ReadU32(data, ref pos);
                if (pos + 2 > data.Length) break;
                ushort nameLen = ReadU16(data, ref pos);
                if (nameLen == 0 || pos + nameLen > data.Length) continue;
                string name = Encoding.UTF8.GetString(data, pos, nameLen);
                pos += nameLen;

                ulong key = ((ulong)instHi << 32) | (ulong)instLo;
                DbpfEntry entry;
                if (map.TryGetValue(key, out entry))
                {
                    if (string.IsNullOrEmpty(entry.ResolvedName))
                        entry.ResolvedName = name;
                }
            }
        }

        /// <summary>
        /// STBL format (Sims 3 String Table):
        ///   uint32 magic (0x5354424C "STBL")
        ///   uint16 version
        ///   uint32 count
        ///   For each:
        ///     uint32 hash (FNV of the string key)
        ///     uint16 strLen
        ///     byte[strLen] str (UTF-8)
        /// </summary>
        private static void ParseSTBL(byte[] data, DbpfEntry stblEntry, Dictionary<ulong, DbpfEntry> map)
        {
            if (data.Length < 10) return;
            int pos = 0;
            uint magic = ReadU32(data, ref pos);
            // Accept STBL magic or just try parsing
            ushort version = ReadU16(data, ref pos);
            uint count = ReadU32(data, ref pos);
            if (count > 500000) return;

            // STBL maps hash -> string. We can try to match hashes to instance IDs.
            // Store them for potential cross-referencing.
            Dictionary<uint, string> hashMap = new Dictionary<uint, string>();

            for (uint i = 0; i < count && pos + 6 <= data.Length; i++)
            {
                uint hash = ReadU32(data, ref pos);
                if (pos + 2 > data.Length) break;
                ushort strLen = ReadU16(data, ref pos);
                if (strLen == 0 || pos + strLen > data.Length) { pos += strLen; continue; }
                string str = Encoding.UTF8.GetString(data, pos, strLen);
                pos += strLen;
                if (!hashMap.ContainsKey(hash))
                    hashMap[hash] = str;
            }

            // Try to match: some entries use the low 32 bits of instance as hash
            foreach (KeyValuePair<ulong, DbpfEntry> kv in map)
            {
                DbpfEntry e = kv.Value;
                if (!string.IsNullOrEmpty(e.ResolvedName)) continue;
                string name;
                if (hashMap.TryGetValue(e.InstanceLo, out name))
                {
                    e.ResolvedName = name;
                }
            }
        }

        /// <summary>
        /// XML Manifest: contains &lt;resource&gt; elements with type/group/instance
        /// and sometimes a "name" attribute.
        /// Simple regex-free parser for basic XML.
        /// </summary>
        private static void ParseXMLManifest(byte[] data, Dictionary<ulong, DbpfEntry> map)
        {
            string xml;
            try { xml = Encoding.UTF8.GetString(data); }
            catch { return; }

            // Very simple extraction: look for name="..." near instance="..."
            // This is a best-effort approach without a full XML parser.
            int searchFrom = 0;
            while (searchFrom < xml.Length)
            {
                int resIdx = xml.IndexOf("<resource", searchFrom, StringComparison.OrdinalIgnoreCase);
                if (resIdx < 0) break;
                int endIdx = xml.IndexOf(">", resIdx);
                if (endIdx < 0) break;
                string tag = xml.Substring(resIdx, endIdx - resIdx + 1);

                string nameVal = ExtractXmlAttribute(tag, "name");
                string instVal = ExtractXmlAttribute(tag, "instance");

                if (!string.IsNullOrEmpty(nameVal) && !string.IsNullOrEmpty(instVal))
                {
                    ulong inst;
                    if (TryParseHexOrDec(instVal, out inst))
                    {
                        DbpfEntry entry;
                        if (map.TryGetValue(inst, out entry) && string.IsNullOrEmpty(entry.ResolvedName))
                            entry.ResolvedName = nameVal;
                    }
                }
                searchFrom = endIdx + 1;
            }
        }

        /// <summary>
        /// Spore locale: typically a binary table of hash -> string.
        /// Format varies; attempt basic parsing.
        /// </summary>
        private static void ParseSporeLocale(byte[] data, DbpfEntry localeEntry, Dictionary<ulong, DbpfEntry> map)
        {
            // Spore locale files often have:
            //   uint32 count
            //   For each: uint32 hash, uint16 len, byte[len] utf8 string
            if (data.Length < 4) return;
            int pos = 0;
            uint count = ReadU32(data, ref pos);
            if (count > 200000) return;

            Dictionary<uint, string> hashMap = new Dictionary<uint, string>();
            for (uint i = 0; i < count && pos + 6 <= data.Length; i++)
            {
                uint hash = ReadU32(data, ref pos);
                if (pos + 2 > data.Length) break;
                ushort len = ReadU16(data, ref pos);
                if (len == 0 || pos + len > data.Length) { pos += len; continue; }
                string str = Encoding.UTF8.GetString(data, pos, len);
                pos += len;
                if (!hashMap.ContainsKey(hash))
                    hashMap[hash] = str;
            }

            foreach (KeyValuePair<ulong, DbpfEntry> kv in map)
            {
                DbpfEntry e = kv.Value;
                if (!string.IsNullOrEmpty(e.ResolvedName)) continue;
                string name;
                if (hashMap.TryGetValue(e.InstanceLo, out name))
                    e.ResolvedName = name;
                else if (hashMap.TryGetValue((uint)(e.Instance & 0xFFFFFFFF), out name))
                    e.ResolvedName = name;
            }
        }

        private static string ExtractXmlAttribute(string tag, string attrName)
        {
            string search = attrName + "=\"";
            int idx = tag.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                search = attrName + "='";
                idx = tag.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            }
            if (idx < 0) return null;
            int start = idx + search.Length;
            char quote = search[search.Length - 1];
            int end = tag.IndexOf(quote, start);
            if (end < 0) return null;
            return tag.Substring(start, end - start);
        }

        private static bool TryParseHexOrDec(string s, out ulong result)
        {
            result = 0;
            if (string.IsNullOrEmpty(s)) return false;
            s = s.Trim();
            try
            {
                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    result = ulong.Parse(s.Substring(2), NumberStyles.HexNumber);
                    return true;
                }
                result = ulong.Parse(s);
                return true;
            }
            catch { return false; }
        }

        private static uint ReadU32(byte[] data, ref int pos)
        {
            if (pos + 4 > data.Length) { pos = data.Length; return 0; }
            uint v = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
            pos += 4;
            return v;
        }

        private static ushort ReadU16(byte[] data, ref int pos)
        {
            if (pos + 2 > data.Length) { pos = data.Length; return 0; }
            ushort v = (ushort)(data[pos] | (data[pos + 1] << 8));
            pos += 2;
            return v;
        }
    }

    // ========================================================================
    //  7. QFS/REFPACK DECOMPRESSOR
    // ========================================================================
    internal static class QfsDecompressor
    {
        public static byte[] Decompress(byte[] data, uint expectedSize)
        {
            if (data == null || data.Length < 2)
                return data;

            int pos = 0;
            uint uncompSize = expectedSize;
            bool hasHeader = false;

            // Detect header format
            if (data.Length >= 5)
            {
                ushort sig = (ushort)(data[0] | (data[1] << 8));

                if (sig == DbpfConst.COMPRESS_REFPACK || sig == DbpfConst.COMPRESS_REFPACK2
                    || sig == DbpfConst.COMPRESS_REFPACK3)
                {
                    hasHeader = true;
                    // Check for 0x80 flag (4-byte size)
                    if ((data[0] & 0x80) != 0 && data.Length >= 6)
                    {
                        uncompSize = (uint)((data[2] << 24) | (data[3] << 16) | (data[4] << 8) | data[5]);
                        pos = 6;
                    }
                    else
                    {
                        uncompSize = (uint)((data[2] << 16) | (data[3] << 8) | data[4]);
                        pos = 5;
                    }
                }
            }

            if (!hasHeader && data.Length >= 9)
            {
                // Sims2-style: 4-byte compressed size + 2-byte sig + 3-byte uncompressed
                ushort sig2 = (ushort)(data[4] | (data[5] << 8));
                if (sig2 == DbpfConst.COMPRESS_REFPACK || sig2 == DbpfConst.COMPRESS_REFPACK2
                    || sig2 == DbpfConst.COMPRESS_REFPACK3)
                {
                    hasHeader = true;
                    uncompSize = (uint)((data[6] << 16) | (data[7] << 8) | data[8]);
                    pos = 9;
                }
            }

            if (!hasHeader)
            {
                pos = 0;
                if (expectedSize > 0) uncompSize = expectedSize;
                else uncompSize = (uint)(data.Length * 4);
            }

            if (uncompSize == 0) uncompSize = (uint)(data.Length * 4);
            if (uncompSize > 512u * 1024 * 1024) uncompSize = 512u * 1024 * 1024; // 512MB cap

            byte[] output = new byte[uncompSize];
            int outPos = 0;

            try
            {
                while (pos < data.Length && outPos < output.Length)
                {
                    byte cc = data[pos];
                    pos++;

                    if (cc >= 0xFC)
                    {
                        // 0xFC-0xFF: plain text, end marker
                        int numPlain = cc & 0x03;
                        for (int i = 0; i < numPlain && pos < data.Length && outPos < output.Length; i++)
                            output[outPos++] = data[pos++];
                        break; // end of compressed data
                    }
                    else if (cc >= 0xE0)
                    {
                        // 0xE0-0xFB: plain text (multiple of 4)
                        int numPlain = ((cc & 0x1F) << 2) + 4;
                        for (int i = 0; i < numPlain && pos < data.Length && outPos < output.Length; i++)
                            output[outPos++] = data[pos++];
                    }
                    else if (cc >= 0xC0)
                    {
                        // 0xC0-0xDF: 4-byte control
                        if (pos + 2 >= data.Length) break;
                        byte b1 = data[pos++];
                        byte b2 = data[pos++];
                        byte b3 = data[pos++];
                        int numPlain = cc & 0x03;
                        int numCopy = ((cc & 0x0C) << 6) + b3 + 5;
                        int offset = ((cc & 0x10) << 12) + (b1 << 8) + b2 + 1;

                        for (int i = 0; i < numPlain && pos < data.Length && outPos < output.Length; i++)
                            output[outPos++] = data[pos++];

                        int srcPos = outPos - offset;
                        for (int i = 0; i < numCopy && outPos < output.Length; i++)
                        {
                            if (srcPos + i >= 0 && srcPos + i < outPos)
                                output[outPos] = output[srcPos + i];
                            outPos++;
                        }
                    }
                    else if (cc >= 0x80)
                    {
                        // 0x80-0xBF: 3-byte control
                        if (pos + 1 >= data.Length) break;
                        byte b1 = data[pos++];
                        byte b2 = data[pos++];
                        int numPlain = (b1 >> 6) & 0x03;
                        int numCopy = (cc & 0x3F) + 4;
                        int offset = ((b1 & 0x3F) << 8) + b2 + 1;

                        for (int i = 0; i < numPlain && pos < data.Length && outPos < output.Length; i++)
                            output[outPos++] = data[pos++];

                        int srcPos = outPos - offset;
                        for (int i = 0; i < numCopy && outPos < output.Length; i++)
                        {
                            if (srcPos + i >= 0 && srcPos + i < outPos)
                                output[outPos] = output[srcPos + i];
                            outPos++;
                        }
                    }
                    else
                    {
                        // 0x00-0x7F: 2-byte control
                        if (pos >= data.Length) break;
                        byte b1 = data[pos++];
                        int numPlain = cc & 0x03;
                        int numCopy = ((cc & 0x1C) >> 2) + 3;
                        int offset = ((cc & 0x60) << 3) + b1 + 1;

                        for (int i = 0; i < numPlain && pos < data.Length && outPos < output.Length; i++)
                            output[outPos++] = data[pos++];

                        int srcPos = outPos - offset;
                        for (int i = 0; i < numCopy && outPos < output.Length; i++)
                        {
                            if (srcPos + i >= 0 && srcPos + i < outPos)
                                output[outPos] = output[srcPos + i];
                            outPos++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Decompression warning at pos " + pos + ": " + ex.Message);
            }

            if (outPos < output.Length)
                Array.Resize(ref output, outPos);
            return output;
        }
    }

    // ========================================================================
    //  8. DBPF DECODER (fault-tolerant reader)
    // ========================================================================
    internal sealed class DbpfDecoder : IDisposable
    {
        private Stream _stream;
        private BinaryReader _reader;
        private DbpfHeader _header;
        private List<DbpfEntry> _entries;
        private string _filePath;
        private bool _disposed;
        private readonly object _readLock = new object();

        public DbpfHeader Header { get { return _header; } }
        public List<DbpfEntry> Entries { get { return _entries; } }
        public int Count { get { return _entries != null ? _entries.Count : 0; } }
        public string FilePath { get { return _filePath; } }

        public DbpfDecoder(string path)
        {
            _filePath = path;
            _entries = new List<DbpfEntry>();
        }

        public bool Open()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    Log.Error("File not found: " + _filePath);
                    return false;
                }

                FileInfo fi = new FileInfo(_filePath);
                if (fi.Length < 4)
                {
                    Log.Error("File too small: " + fi.Length + " bytes");
                    return false;
                }

                _stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite, DbpfConst.IO_BUFFER_SIZE);
                _reader = new BinaryReader(_stream);

                if (_stream.Length < DbpfConst.HEADER_SIZE)
                {
                    Log.Warn("File smaller than header (" + _stream.Length + " bytes), attempting partial read...");
                    // Try to read what we can
                    _stream.Position = 0;
                    byte[] partial = new byte[Math.Min((int)_stream.Length, DbpfConst.HEADER_SIZE)];
                    _stream.Read(partial, 0, partial.Length);
                    // Pad to 96 bytes
                    byte[] padded = new byte[DbpfConst.HEADER_SIZE];
                    Array.Copy(partial, padded, partial.Length);
                    BinaryReader pr = new BinaryReader(new MemoryStream(padded));
                    _header = DbpfHeader.Read(pr);
                }
                else
                {
                    _stream.Position = 0;
                    _header = DbpfHeader.Read(_reader);
                }

                // Validate magic
                if (_header.Magic != DbpfConst.MAGIC_DBPF && _header.Magic != DbpfConst.MAGIC_DBPP)
                {
                    Log.Warn(string.Format("Invalid magic: 0x{0:X8} ('{1}'), attempting recovery...",
                        _header.Magic, _header.GetMagicString()));
                    // Try to continue anyway
                }

                if (_header.IsEncrypted)
                {
                    Log.Warn("Encrypted DBPF (DBPP) detected. Attempting basic decode...");
                    DecryptHeader();
                }

                Log.Info(string.Format("DBPF v{0}, entries={1}, indexSize={2}, indexOffset=0x{3:X8}",
                    _header.GetVersionString(), _header.IndexEntryCount,
                    _header.IndexSize,
                    _header.IsVersion2 ? _header.IndexOffset : _header.IndexFirstEntry));

                // Read index
                if (_header.IsVersion2 || _header.IsVersion3)
                    ReadIndexV2();
                else
                    ReadIndexV1();

                // Read DIR resource for compression info
                ReadDirResource();

                // Resolve names from internal resources
                try { NameResolver.ResolveNames(this); }
                catch (Exception ex) { Log.Debug("Name resolution error: " + ex.Message); }

                Log.Ok(string.Format("Opened: {0} ({1} entries)", Path.GetFileName(_filePath), _entries.Count));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Open failed: " + ex.Message);
                Log.Debug(ex.StackTrace);
                return false;
            }
        }

        private void DecryptHeader()
        {
            try
            {
                _stream.Position = 0;
                byte[] hdr = _reader.ReadBytes(DbpfConst.HEADER_SIZE);
                for (int i = 0; i < hdr.Length; i++)
                    hdr[i] ^= (byte)(i * 7 + 0x5A);
                MemoryStream ms = new MemoryStream(hdr);
                BinaryReader br2 = new BinaryReader(ms);
                _header = DbpfHeader.Read(br2);
                if (_header.Magic == DbpfConst.MAGIC_DBPF)
                    Log.Ok("DBPP header decrypted successfully.");
                else
                    Log.Warn("DBPP decryption may have failed, continuing with original header.");
            }
            catch (Exception ex)
            {
                Log.Warn("DBPP decrypt attempt: " + ex.Message);
            }
        }

        private void ReadIndexV2()
        {
            uint indexOffset = _header.IndexOffset;
            uint entryCount = _header.IndexEntryCount;

            if (indexOffset == 0 || entryCount == 0)
            {
                Log.Warn("No index table (offset=0 or count=0).");
                return;
            }

            if (indexOffset >= _stream.Length)
            {
                Log.Error(string.Format("Index offset 0x{0:X8} beyond file size 0x{1:X8}",
                    indexOffset, _stream.Length));
                // Try to find index by scanning backwards
                TryRecoverIndex();
                return;
            }

            _stream.Position = indexOffset;

            uint indexType;
            try { indexType = _reader.ReadUInt32(); }
            catch
            {
                Log.Error("Cannot read index type at offset 0x" + indexOffset.ToString("X8"));
                return;
            }

            Log.Debug(string.Format("Index type: 0x{0:X8} (bits: {1})",
                indexType, Convert.ToString(indexType, 2).PadLeft(8, '0')));

            int setBits = 0;
            for (int b = 0; b < 8; b++)
                if ((indexType & (1u << b)) != 0) setBits++;
            int unsetBits = 8 - setBits;

            // Read Index Header (constant values for SET bits)
            uint[] headerValues = new uint[setBits];
            for (int i = 0; i < setBits; i++)
            {
                if (_stream.Position + 4 <= _stream.Length)
                    headerValues[i] = _reader.ReadUInt32();
                else
                {
                    Log.Warn("Truncated index header at field " + i);
                    return;
                }
            }

            // Read entries
            int errors = 0;
            for (uint i = 0; i < entryCount; i++)
            {
                try
                {
                    if (_stream.Position + (unsetBits * 4) > _stream.Length)
                    {
                        Log.Warn(string.Format("Truncated index at entry {0}/{1}", i, entryCount));
                        break;
                    }

                    DbpfEntry entry = new DbpfEntry();
                    entry.Index = (int)i;

                    uint[] entryValues = new uint[unsetBits];
                    for (int j = 0; j < unsetBits; j++)
                        entryValues[j] = _reader.ReadUInt32();

                    int hdrIdx = 0, entIdx = 0;
                    uint[] fieldValues = new uint[8];
                    for (int bit = 0; bit < 8; bit++)
                    {
                        if ((indexType & (1u << bit)) != 0)
                        {
                            if (hdrIdx < headerValues.Length)
                                fieldValues[bit] = headerValues[hdrIdx++];
                        }
                        else
                        {
                            if (entIdx < entryValues.Length)
                                fieldValues[bit] = entryValues[entIdx++];
                        }
                    }

                    entry.ResourceType = fieldValues[0];
                    entry.ResourceGroup = fieldValues[1];
                    entry.InstanceHi = fieldValues[2];
                    entry.InstanceLo = fieldValues[3];
                    entry.Offset = fieldValues[4];
                    entry.FileSize = fieldValues[5];
                    entry.MemSize = fieldValues[6];
                    entry.Compressed = (ushort)(fieldValues[7] & 0xFFFF);
                    entry.Unknown = (ushort)(fieldValues[7] >> 16);

                    // Validate
                    if (entry.Offset < _stream.Length || entry.PackedSize == 0)
                        _entries.Add(entry);
                    else
                    {
                        Log.Debug(string.Format("Entry {0}: offset 0x{1:X8} beyond file, skipping",
                            i, entry.Offset));
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    if (errors <= 3)
                        Log.Debug(string.Format("Entry {0} read error: {1}", i, ex.Message));
                    if (errors > 100)
                    {
                        Log.Warn("Too many index errors, stopping at entry " + i);
                        break;
                    }
                }
            }
            if (errors > 0)
                Log.Warn("Index read completed with " + errors + " error(s).");
        }

        private void ReadIndexV1()
        {
            uint indexOffset = _header.IndexFirstEntry;
            uint entryCount = _header.IndexEntryCount;
            uint indexMinor = _header.IndexMinorVersion;

            if (indexOffset == 0 || entryCount == 0)
            {
                Log.Warn("No index table in v1 file.");
                return;
            }

            if (indexOffset >= _stream.Length)
            {
                Log.Error(string.Format("Index offset 0x{0:X8} beyond file size", indexOffset));
                TryRecoverIndex();
                return;
            }

            _stream.Position = indexOffset;

            // Determine entry size: 7.0=20 bytes, 7.1=24 bytes
            int entrySize = (indexMinor >= 2) ? 24 : 20;
            bool hasSecondInstance = (indexMinor >= 2);

            Log.Debug(string.Format("Index v7.{0}, entrySize={1}, count={2}",
                indexMinor, entrySize, entryCount));

            int errors = 0;
            for (uint i = 0; i < entryCount; i++)
            {
                try
                {
                    if (_stream.Position + entrySize > _stream.Length)
                    {
                        Log.Warn(string.Format("Truncated index at entry {0}/{1}", i, entryCount));
                        break;
                    }

                    DbpfEntry entry = new DbpfEntry();
                    entry.Index = (int)i;
                    entry.ResourceType = _reader.ReadUInt32();
                    entry.ResourceGroup = _reader.ReadUInt32();
                    entry.InstanceLo = _reader.ReadUInt32();
                    entry.InstanceHi = 0;
                    if (hasSecondInstance)
                        entry.InstanceHi = _reader.ReadUInt32();
                    entry.Offset = _reader.ReadUInt32();
                    entry.FileSize = _reader.ReadUInt32();
                    entry.MemSize = entry.PackedSize;
                    entry.Compressed = 0;

                    if (entry.Offset < _stream.Length || entry.PackedSize == 0)
                        _entries.Add(entry);
                }
                catch (Exception ex)
                {
                    errors++;
                    if (errors <= 3)
                        Log.Debug(string.Format("v1 Entry {0} error: {1}", i, ex.Message));
                    if (errors > 100) break;
                }
            }
        }

        private void TryRecoverIndex()
        {
            Log.Warn("Attempting index recovery by scanning for index patterns...");
            // Heuristic: scan backwards from end of file for plausible index data
            // This is a best-effort recovery for corrupted files
            try
            {
                long scanStart = Math.Max(0, _stream.Length - 1024 * 1024); // scan last 1MB
                _stream.Position = scanStart;
                byte[] scanBuf = new byte[_stream.Length - scanStart];
                _stream.Read(scanBuf, 0, scanBuf.Length);

                // Look for a sequence that could be an index type DWORD followed by valid entries
                for (int i = 0; i < scanBuf.Length - 8; i += 4)
                {
                    uint possibleType = (uint)(scanBuf[i] | (scanBuf[i + 1] << 8) |
                        (scanBuf[i + 2] << 16) | (scanBuf[i + 3] << 24));
                    if (possibleType <= 0xFF) // plausible index type
                    {
                        long candidateOffset = scanStart + i;
                        Log.Debug(string.Format("Possible index at 0x{0:X8} (type=0x{1:X2})",
                            candidateOffset, possibleType));
                        // We could try to parse from here, but for safety just log
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Index recovery scan error: " + ex.Message);
            }
        }

        private void ReadDirResource()
        {
            DbpfEntry dirEntry = null;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].ResourceType == DbpfConst.DIR_RESOURCE_TYPE)
                {
                    dirEntry = _entries[i];
                    break;
                }
            }
            if (dirEntry == null) return;

            try
            {
                byte[] dirData = ReadEntryData(dirEntry);
                if (dirData == null || dirData.Length < 12) return;

                BinaryReader br = new BinaryReader(new MemoryStream(dirData));
                int dirEntrySize = _header.IsVersion2 ? 20 : 16;
                int dirCount = dirData.Length / dirEntrySize;

                for (int i = 0; i < dirCount; i++)
                {
                    if (br.BaseStream.Position + dirEntrySize > br.BaseStream.Length) break;
                    uint type = br.ReadUInt32();
                    uint group = br.ReadUInt32();
                    uint instLo, instHi = 0;
                    if (_header.IsVersion2)
                    {
                        instHi = br.ReadUInt32();
                        instLo = br.ReadUInt32();
                    }
                    else
                    {
                        instLo = br.ReadUInt32();
                    }
                    uint memSize = br.ReadUInt32();

                    for (int j = 0; j < _entries.Count; j++)
                    {
                        DbpfEntry e = _entries[j];
                        if (e.ResourceType == type && e.ResourceGroup == group &&
                            e.InstanceLo == instLo && e.InstanceHi == instHi)
                        {
                            e.Compressed = 0xFFFF;
                            e.MemSize = memSize;
                            break;
                        }
                    }
                }
                Log.Debug("DIR resource processed: " + dirCount + " compressed entries marked.");
            }
            catch (Exception ex)
            {
                Log.Debug("DIR read error: " + ex.Message);
            }
        }

        public byte[] ReadEntryData(DbpfEntry entry)
        {
            lock (_readLock)
            {
                try
                {
                    if (entry.Offset >= _stream.Length) return null;
                    _stream.Position = entry.Offset;
                    uint size = entry.PackedSize;
                    if (size == 0) size = entry.FileSize;
                    if (size == 0) return new byte[0];

                    if (entry.Offset + size > _stream.Length)
                    {
                        size = (uint)(_stream.Length - entry.Offset);
                        Log.Debug(string.Format("Entry {0}: clamped size to {1}", entry.Index, size));
                    }

                    byte[] raw = new byte[size];
                    int totalRead = 0;
                    while (totalRead < size)
                    {
                        int r = _stream.Read(raw, totalRead, (int)(size - totalRead));
                        if (r <= 0) break;
                        totalRead += r;
                    }
                    if (totalRead < size)
                        Array.Resize(ref raw, totalRead);

                    if (entry.IsCompressed && raw.Length > 5)
                    {
                        try
                        {
                            return QfsDecompressor.Decompress(raw, entry.MemSize);
                        }
                        catch (Exception ex)
                        {
                            Log.Warn(string.Format("Decompress failed for entry {0}: {1}",
                                entry.Index, ex.Message));
                            return raw;
                        }
                    }
                    return raw;
                }
                catch (Exception ex)
                {
                    Log.Error(string.Format("ReadEntryData error (entry {0}): {1}",
                        entry.Index, ex.Message));
                    return null;
                }
            }
        }

        public void ExtractEntry(DbpfEntry entry, string destPath, IProgressReporter progress)
        {
            byte[] data = ReadEntryData(entry);
            if (data == null)
            {
                Log.Error("No data for entry " + entry.Index);
                return;
            }

            string dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (FileStream fs = new FileStream(destPath, FileMode.Create,
                FileAccess.Write, FileShare.None, DbpfConst.IO_BUFFER_SIZE))
            {
                fs.Write(data, 0, data.Length);
            }

            if (progress != null)
                progress.Report(100, data.Length, data.Length);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { if (_reader != null) _reader.Close(); } catch { }
            try { if (_stream != null) _stream.Dispose(); } catch { }
        }
    }

    // ========================================================================
    //  9. DBPF CODER (writer)
    // ========================================================================
    internal sealed class DbpfCoder : IDisposable
    {
        private Stream _stream;
        private BinaryWriter _writer;
        private List<DbpfEntry> _entries;
        private List<byte[]> _entryData;
        private string _filePath;

        public DbpfCoder(string path)
        {
            _filePath = path;
            _entries = new List<DbpfEntry>();
            _entryData = new List<byte[]>();
        }

        public void AddEntry(uint type, uint group, uint instHi, uint instLo, byte[] data)
        {
            DbpfEntry e = new DbpfEntry();
            e.ResourceType = type;
            e.ResourceGroup = group;
            e.InstanceHi = instHi;
            e.InstanceLo = instLo;
            e.FileSize = (uint)data.Length;
            e.MemSize = (uint)data.Length;
            e.Compressed = 0;
            _entries.Add(e);
            _entryData.Add(data);
        }

        public bool Save()
        {
            try
            {
                _stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write,
                    FileShare.None, DbpfConst.IO_BUFFER_SIZE);
                _writer = new BinaryWriter(_stream);

                // Header
                _writer.Write(DbpfConst.MAGIC_DBPF);
                _writer.Write((uint)2);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write((uint)_entries.Count);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write((uint)3);
                _writer.Write((uint)0);
                _writer.Write((uint)0);
                _writer.Write(new byte[24]);

                uint dataOffset = DbpfConst.HEADER_SIZE;
                for (int i = 0; i < _entries.Count; i++)
                {
                    _entries[i].Offset = dataOffset;
                    _writer.Write(_entryData[i]);
                    dataOffset += (uint)_entryData[i].Length;
                }

                uint indexOffset = dataOffset;
                _writer.Write((uint)0x00); // index type: all per-entry
                for (int i = 0; i < _entries.Count; i++)
                {
                    DbpfEntry e = _entries[i];
                    _writer.Write(e.ResourceType);
                    _writer.Write(e.ResourceGroup);
                    _writer.Write(e.InstanceHi);
                    _writer.Write(e.InstanceLo);
                    _writer.Write(e.Offset);
                    _writer.Write(e.FileSize);
                    _writer.Write(e.MemSize);
                    _writer.Write((uint)((e.Unknown << 16) | e.Compressed));
                }
                uint indexSize = (uint)(_stream.Position - indexOffset);

                _stream.Position = 36;
                _writer.Write((uint)_entries.Count);
                _stream.Position = 44;
                _writer.Write(indexSize);
                _stream.Position = 64;
                _writer.Write(indexOffset);
                _writer.Flush();

                Log.Ok("Saved: " + _filePath + " (" + _entries.Count + " entries)");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Save failed: " + ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
            try { if (_writer != null) _writer.Close(); } catch { }
            try { if (_stream != null) _stream.Dispose(); } catch { }
        }
    }

    // ========================================================================
    //  10. PROGRESS REPORTER
    // ========================================================================
    internal interface IProgressReporter
    {
        void Report(int percent, long done, long total);
    }

    // ========================================================================
    //  11. CHECKPOINT / STATE MANAGER
    // ========================================================================
    internal static class StateManager
    {
        private static string _stateFile;
        private static ThreadingTimer _checkpointTimer;

        public static void Init()
        {
            _stateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "eapb_state.dat");
            _checkpointTimer = new ThreadingTimer(
                CheckpointCallback, null,
                DbpfConst.CHECKPOINT_INTERVAL_MS,
                DbpfConst.CHECKPOINT_INTERVAL_MS);
        }

        private static void CheckpointCallback(object state)
        {
            try { SaveState(); } catch { }
        }

        public static void SaveState()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(_stateFile, false, Encoding.UTF8))
                {
                    sw.WriteLine("EAPB_STATE_V1");
                    sw.WriteLine(DateTime.Now.ToString("o"));
                    sw.WriteLine(SessionState.LastOpenedFile ?? "");
                    sw.WriteLine(SessionState.LastSelectedIndex);
                    sw.WriteLine(SessionState.ConsoleMode ? "console" : "gui");
                }
            }
            catch { }
        }

        public static void LoadState()
        {
            try
            {
                if (!File.Exists(_stateFile)) return;
                using (StreamReader sr = new StreamReader(_stateFile, Encoding.UTF8))
                {
                    string ver = sr.ReadLine();
                    if (ver != "EAPB_STATE_V1") return;
                    sr.ReadLine();
                    SessionState.LastOpenedFile = sr.ReadLine();
                    string idxStr = sr.ReadLine();
                    int idx;
                    if (int.TryParse(idxStr, out idx))
                        SessionState.LastSelectedIndex = idx;
                    string mode = sr.ReadLine();
                    SessionState.ConsoleMode = (mode == "console");
                }
                Log.Info("State restored from checkpoint.");
            }
            catch { }
        }

        public static void Shutdown()
        {
            try { if (_checkpointTimer != null) _checkpointTimer.Dispose(); } catch { }
            SaveState();
        }
    }

    internal static class SessionState
    {
        public static string LastOpenedFile = null;
        public static int LastSelectedIndex = 0;
        public static bool ConsoleMode = false;
    }

    // ========================================================================
    //  12. TUI HELPERS
    // ========================================================================
    internal static class Tui
    {
        public static void SetTitle(string status)
        {
            try { Console.Title = "EA Package Browser — " + status; } catch { }
        }

        public static void DrawProgressBar(int percent, int width, string label)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            int filled = width * percent / 100;
            StringBuilder sb = new StringBuilder();
            sb.Append("  [");
            for (int i = 0; i < width; i++)
                sb.Append(i < filled ? '\u2588' : '\u2591');
            sb.Append("] ");
            sb.Append(percent.ToString().PadLeft(3));
            sb.Append("% ");
            sb.Append(label ?? "");
            Console.Write("\r" + sb.ToString());
            if (percent >= 100) Console.WriteLine();
        }

        public static void Header(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("  +" + new string('=', text.Length + 2) + "+");
            Console.WriteLine("  | " + text + " |");
            Console.WriteLine("  +" + new string('=', text.Length + 2) + "+");
            Console.ResetColor();
        }

        public static int ShowMenu(string title, string[] options)
        {
            int sel = 0;
            ConsoleKey key;
            do
            {
                Console.Clear();
                Header(title);
                Console.WriteLine();
                for (int i = 0; i < options.Length; i++)
                {
                    if (i == sel)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.WriteLine("  > " + options[i]);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("    " + options[i]);
                    }
                }
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  Up/Down: select | Enter: confirm | Esc: back");
                Console.ResetColor();
                key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.UpArrow && sel > 0) sel--;
                else if (key == ConsoleKey.DownArrow && sel < options.Length - 1) sel++;
            } while (key != ConsoleKey.Enter && key != ConsoleKey.Escape);
            return key == ConsoleKey.Escape ? -1 : sel;
        }

        public static string Prompt(string label)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  " + label + ": ");
            Console.ResetColor();
            return Console.ReadLine();
        }

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
        }
    }

    internal sealed class ConsoleProgress : IProgressReporter
    {
        private string _label;
        public ConsoleProgress(string label) { _label = label; }
        public void Report(int percent, long done, long total)
        {
            Tui.DrawProgressBar(percent, 40,
                _label + " " + Tui.FormatSize(done) + " / " + Tui.FormatSize(total));
        }
    }

    // ========================================================================
    //  13. DOUBLE-BUFFERED LISTVIEW (anti-flicker)
    // ========================================================================
    internal sealed class BufferedListView : ListView
    {
        public BufferedListView()
        {
            this.DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }
    }

    // ========================================================================
    //  14. WINFORMS GUI (with BackgroundWorker for smooth loading)
    // ========================================================================
    internal sealed class GuiProgress : IProgressReporter
    {
        private ProgressBar _bar;
        private ToolStripStatusLabel _label;

        public GuiProgress(ProgressBar bar, ToolStripStatusLabel label)
        {
            _bar = bar;
            _label = label;
        }

        public void Report(int percent, long done, long total)
        {
            if (_bar.InvokeRequired)
                _bar.BeginInvoke((MethodInvoker)delegate { SetVal(percent, done, total); });
            else
                SetVal(percent, done, total);
        }

        private void SetVal(int p, long d, long t)
        {
            _bar.Value = Math.Max(0, Math.Min(100, p));
            _label.Text = Tui.FormatSize(d) + " / " + Tui.FormatSize(t);
        }
    }

    internal sealed class MainForm : Form
    {
        private TreeView _tree;
        private BufferedListView _list;
        private SplitContainer _split;
        private MenuStrip _menu;
        private StatusStrip _status;
        private ToolStripStatusLabel _statusLabel;
        private ProgressBar _progressBar;
        private ImageList _images;
        private ContextMenuStrip _ctxList;
        private BackgroundWorker _bgWorker;
        private DbpfDecoder _decoder;
        private string _currentFile;
        private List<DbpfEntry> _filteredEntries;
        private bool _virtualMode;

        public MainForm()
        {
            BuildUi();
            Tui.SetTitle("Ready");
            _statusLabel.Text = "Open a .package file to begin.";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                int target = 220;
                int lo = _split.Panel1MinSize;
                int hi = _split.Width - _split.Panel2MinSize - _split.SplitterWidth;
                if (target < lo) target = lo;
                if (target > hi) target = hi;
                _split.SplitterDistance = target;
            }
            catch { }
            try { _split.Panel1MinSize = 120; } catch { }
            try { _split.Panel2MinSize = 200; } catch { }

            if (!string.IsNullOrEmpty(SessionState.LastOpenedFile) && File.Exists(SessionState.LastOpenedFile))
                OpenPackage(SessionState.LastOpenedFile);
        }

        private void BuildUi()
        {
            Text = "EA Package Browser v2.0";
            MinimumSize = new Size(950, 600);
            StartPosition = FormStartPosition.CenterScreen;

            _images = new ImageList();
            _images.ImageSize = new Size(16, 16);
            _images.Images.Add("package", MakePackageIcon());
            _images.Images.Add("resource", MakeResourceIcon());
            _images.Images.Add("compressed", MakeCompressedIcon());
            _images.Images.Add("folder", MakeFolderIcon());
            _images.Images.Add("named", MakeNamedIcon());

            // Menu
            _menu = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("&Open Package...", null, OnOpenPackage);
            fileMenu.DropDownItems.Add("Open &Folder (recursive)...", null, OnOpenFolder);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("&Close", null, OnClosePackage);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("E&xit", null, delegate { Close(); });

            ToolStripMenuItem toolsMenu = new ToolStripMenuItem("&Tools");
            toolsMenu.DropDownItems.Add("&Extract Selected...", null, OnExtractSelected);
            toolsMenu.DropDownItems.Add("Extract &All...", null, OnExtractAll);
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add("&Search by Type...", null, OnSearchType);
            toolsMenu.DropDownItems.Add("Search by &Name...", null, OnSearchName);

            ToolStripMenuItem helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add("&About", null, delegate { ShowAbout(); });

            _menu.Items.Add(fileMenu);
            _menu.Items.Add(toolsMenu);
            _menu.Items.Add(helpMenu);

            // Tree
            _tree = new TreeView();
            _tree.Dock = DockStyle.Fill;
            _tree.ImageList = _images;
            _tree.AfterSelect += OnTreeSelect;
            _tree.HideSelection = false;

            // List (buffered, virtual mode for large sets)
            _list = new BufferedListView();
            _list.Dock = DockStyle.Fill;
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.SmallImageList = _images;
            _list.Columns.Add("Index", 55);
            _list.Columns.Add("Name / Type", 200);
            _list.Columns.Add("Group", 90);
            _list.Columns.Add("Instance", 175);
            _list.Columns.Add("Offset", 90);
            _list.Columns.Add("Size", 80);
            _list.Columns.Add("MemSize", 80);
            _list.Columns.Add("Comp", 50);

            _ctxList = new ContextMenuStrip();
            _ctxList.Items.Add("Extract...", null, OnExtractSelected);
            _ctxList.Items.Add("Extract All...", null, OnExtractAll);
            _ctxList.Items.Add(new ToolStripSeparator());
            _ctxList.Items.Add("View Hex (first 256 bytes)", null, OnViewHex);
            _ctxList.Items.Add("View as Text", null, OnViewText);
            _ctxList.Items.Add("Copy Instance ID", null, OnCopyInstance);
            _list.ContextMenuStrip = _ctxList;
            _list.ItemActivate += OnListActivate;

            // Virtual mode callbacks
            _list.RetrieveVirtualItem += OnRetrieveVirtualItem;
            _list.SearchForVirtualItem += OnSearchForVirtualItem;

            // Split
            _split = new SplitContainer();
            _split.Dock = DockStyle.Fill;
            _split.Panel1.Controls.Add(_tree);
            _split.Panel2.Controls.Add(_list);
            _split.FixedPanel = FixedPanel.Panel1;
            _split.SplitterWidth = 6;

            // Status
            _status = new StatusStrip();
            _progressBar = new ProgressBar();
            _progressBar.Size = new Size(150, 16);
            _progressBar.Visible = false;
            _statusLabel = new ToolStripStatusLabel("Ready");
            _statusLabel.Spring = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _status.Items.Add(_statusLabel);
            _status.Items.Add(new ToolStripControlHost(_progressBar));

            // BackgroundWorker
            _bgWorker = new BackgroundWorker();
            _bgWorker.WorkerReportsProgress = true;
            _bgWorker.WorkerSupportsCancellation = true;
            _bgWorker.DoWork += BgOpenPackage;
            _bgWorker.ProgressChanged += BgProgressChanged;
            _bgWorker.RunWorkerCompleted += BgOpenCompleted;

            Controls.Add(_split);
            Controls.Add(_status);
            Controls.Add(_menu);
            MainMenuStrip = _menu;
        }

        // Code-generated icons
        private static Bitmap MakePackageIcon()
        {
            Bitmap b = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillRectangle(Brushes.CornflowerBlue, 2, 2, 12, 12);
                g.DrawRectangle(Pens.DarkBlue, 2, 2, 11, 11);
                g.DrawString("P", new Font("Arial", 8, FontStyle.Bold), Brushes.White, 3, 2);
            }
            return b;
        }

        private static Bitmap MakeResourceIcon()
        {
            Bitmap b = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(Brushes.White, 3, 1, 10, 14);
                g.DrawRectangle(Pens.Gray, 3, 1, 9, 13);
                g.DrawLine(Pens.Gray, 10, 1, 13, 4);
            }
            return b;
        }

        private static Bitmap MakeCompressedIcon()
        {
            Bitmap b = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(Brushes.LightYellow, 3, 1, 10, 14);
                g.DrawRectangle(Pens.DarkGoldenrod, 3, 1, 9, 13);
                g.DrawString("Z", new Font("Arial", 7, FontStyle.Bold), Brushes.DarkRed, 4, 3);
            }
            return b;
        }

        private static Bitmap MakeFolderIcon()
        {
            Bitmap b = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(Brushes.Gold, 1, 4, 14, 10);
                g.FillRectangle(Brushes.Gold, 1, 2, 6, 3);
                g.DrawRectangle(Pens.DarkGoldenrod, 1, 4, 13, 9);
            }
            return b;
        }

        private static Bitmap MakeNamedIcon()
        {
            Bitmap b = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(Brushes.LightCyan, 3, 1, 10, 14);
                g.DrawRectangle(Pens.Teal, 3, 1, 9, 13);
                g.DrawString("N", new Font("Arial", 7, FontStyle.Bold), Brushes.DarkGreen, 4, 3);
            }
            return b;
        }

        // Background loading
        private void BgOpenPackage(object sender, DoWorkEventArgs e)
        {
            string path = (string)e.Argument;
            DbpfDecoder dec = new DbpfDecoder(path);
            if (!dec.Open())
            {
                e.Result = null;
                return;
            }
            _bgWorker.ReportProgress(50, "Resolving names...");
            e.Result = dec;
        }

        private void BgProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _statusLabel.Text = (string)e.UserState;
            _progressBar.Value = Math.Min(100, e.ProgressPercentage);
        }

        private void BgOpenCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _progressBar.Visible = false;
            _progressBar.Value = 0;

            if (e.Error != null)
            {
                Log.Error("Background open error: " + e.Error.Message);
                _statusLabel.Text = "Error: " + e.Error.Message;
                return;
            }

            DbpfDecoder dec = e.Result as DbpfDecoder;
            if (dec == null)
            {
                _statusLabel.Text = "Failed to open package.";
                return;
            }

            CloseCurrentDecoder();
            _decoder = dec;
            _currentFile = dec.FilePath;
            SessionState.LastOpenedFile = _currentFile;
            StateManager.SaveState();

            BuildPackageTree();
            PopulateList(null);

            _statusLabel.Text = string.Format("{0} — {1} entries — DBPF v{2}",
                Path.GetFileName(_currentFile), _decoder.Count,
                _decoder.Header.GetVersionString());
            Tui.SetTitle(Path.GetFileName(_currentFile) + " (" + _decoder.Count + " entries)");
            Log.Ok("Package loaded: " + Path.GetFileName(_currentFile));
        }

        // Handlers
        private void OnOpenPackage(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Open DBPF Package";
                ofd.Filter = "Package files (*.package;*.world;*.dbc;*.dbpf)|*.package;*.world;*.dbc;*.dbpf|All files (*.*)|*.*";
                ofd.Multiselect = false;
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                OpenPackage(ofd.FileName);
            }
        }

        private void OpenPackage(string path)
        {
            if (_bgWorker.IsBusy)
            {
                Log.Warn("Already loading a package, please wait...");
                return;
            }

            Tui.SetTitle("Opening: " + Path.GetFileName(path));
            _statusLabel.Text = "Opening " + Path.GetFileName(path) + "...";
            _progressBar.Visible = true;
            _progressBar.Style = ProgressBarStyle.Marquee;
            _bgWorker.RunWorkerAsync(path);
        }

        private void OnOpenFolder(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select folder to scan for packages (recursive)";
                if (fbd.ShowDialog(this) != DialogResult.OK) return;
                ScanFolder(fbd.SelectedPath);
            }
        }

        private void ScanFolder(string folder)
        {
            _tree.Nodes.Clear();
            _list.Items.Clear();
            _filteredEntries = null;
            _virtualMode = false;
            _list.VirtualMode = false;

            TreeNode root = new TreeNode(Path.GetFileName(folder), 3, 3);
            root.Tag = folder;
            ScanFolderRecursive(root, folder);
            _tree.Nodes.Add(root);
            root.Expand();
            _statusLabel.Text = "Scanned: " + folder;
            Tui.SetTitle("Folder: " + Path.GetFileName(folder));
        }

        private void ScanFolderRecursive(TreeNode parent, string folder)
        {
            try
            {
                string[] exts = new string[] { "*.package", "*.world", "*.dbc", "*.dbpf" };
                foreach (string ext in exts)
                {
                    string[] files = Directory.GetFiles(folder, ext);
                    foreach (string f in files)
                    {
                        TreeNode node = new TreeNode(Path.GetFileName(f), 0, 0);
                        node.Tag = f;
                        parent.Nodes.Add(node);
                    }
                }
                string[] dirs = Directory.GetDirectories(folder);
                foreach (string d in dirs)
                {
                    TreeNode sub = new TreeNode(Path.GetFileName(d), 3, 3);
                    sub.Tag = d;
                    parent.Nodes.Add(sub);
                    ScanFolderRecursive(sub, d);
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Scan error in " + folder + ": " + ex.Message);
            }
        }

        private void BuildPackageTree()
        {
            _tree.Nodes.Clear();
            if (_decoder == null) return;

            TreeNode root = new TreeNode(Path.GetFileName(_currentFile), 0, 0);
            root.Tag = "root";

            Dictionary<uint, TreeNode> typeNodes = new Dictionary<uint, TreeNode>();
            for (int i = 0; i < _decoder.Entries.Count; i++)
            {
                DbpfEntry entry = _decoder.Entries[i];
                uint type = entry.ResourceType;
                TreeNode typeNode;
                if (!typeNodes.TryGetValue(type, out typeNode))
                {
                    string typeName = entry.GetTypeName();
                    int count = 0;
                    for (int j = 0; j < _decoder.Entries.Count; j++)
                        if (_decoder.Entries[j].ResourceType == type) count++;
                    typeNode = new TreeNode(typeName + " (" + count + ")", 3, 3);
                    typeNode.Tag = type;
                    typeNodes[type] = typeNode;
                    root.Nodes.Add(typeNode);
                }
            }

            _tree.Nodes.Add(root);
            root.Expand();
        }

        private void PopulateList(uint? filterType)
        {
            PopulateList(filterType, null);
        }

        private void PopulateList(uint? filterType, string nameSearch)
        {
            try
            {
                // 1) Собираем отфильтрованный список
                List<DbpfEntry> filtered = new List<DbpfEntry>();
                if (_decoder != null)
                {
                    for (int i = 0; i < _decoder.Entries.Count; i++)
                    {
                        DbpfEntry entry = _decoder.Entries[i];
                        if (filterType.HasValue && entry.ResourceType != filterType.Value)
                            continue;
                        if (!string.IsNullOrEmpty(nameSearch))
                        {
                            string dn = entry.GetDisplayName();
                            if (dn.IndexOf(nameSearch, StringComparison.OrdinalIgnoreCase) < 0)
                                continue;
                        }
                        filtered.Add(entry);
                    }
                }
                _filteredEntries = filtered;

                // 2) Определяем нужен ли virtual mode
                bool wantVirtual = filtered.Count > DbpfConst.VIRTUAL_LIST_THRESHOLD;

                // 3) БЕЗОПАСНОЕ переключение режима:
                //    Сначала ВСЕГДА очищаем элементы, потом меняем VirtualMode
                _list.BeginUpdate();
                try
                {
                    _list.Items.Clear();

                    if (wantVirtual && !_virtualMode)
                    {
                        // Переход в virtual mode
                        try
                        {
                            _list.VirtualMode = true;
                            _virtualMode = true;
                        }
                        catch (InvalidOperationException)
                        {
                            // Fallback: если не удалось — работаем в обычном режиме
                            Log.Warn("VirtualMode switch failed, falling back to normal mode.");
                            _virtualMode = false;
                            wantVirtual = false;
                        }
                    }
                    else if (!wantVirtual && _virtualMode)
                    {
                        // Переход из virtual mode в обычный
                        try
                        {
                            _list.VirtualMode = false;
                            _virtualMode = false;
                        }
                        catch (InvalidOperationException)
                        {
                            Log.Warn("VirtualMode disable failed, forcing clear.");
                            _list.Items.Clear();
                            _list.VirtualMode = false;
                            _virtualMode = false;
                        }
                    }

                    // 4) Заполняем
                    if (_virtualMode)
                    {
                        _list.VirtualListSize = _filteredEntries.Count;
                    }
                    else
                    {
                        for (int i = 0; i < _filteredEntries.Count; i++)
                        {
                            DbpfEntry entry = _filteredEntries[i];
                            string icon;
                            if (!string.IsNullOrEmpty(entry.ResolvedName))
                                icon = "named";
                            else if (entry.IsCompressed)
                                icon = "compressed";
                            else
                                icon = "resource";

                            ListViewItem lvi = new ListViewItem(entry.Index.ToString(), icon);
                            lvi.SubItems.Add(entry.GetDisplayName());
                            lvi.SubItems.Add(entry.GetGroupString());
                            lvi.SubItems.Add(entry.GetInstanceString());
                            lvi.SubItems.Add(string.Format("0x{0:X8}", entry.Offset));
                            lvi.SubItems.Add(Tui.FormatSize(entry.PackedSize));
                            lvi.SubItems.Add(Tui.FormatSize(entry.MemSize));
                            lvi.SubItems.Add(entry.IsCompressed ? "Yes" : "No");
                            lvi.Tag = entry;
                            _list.Items.Add(lvi);
                        }
                    }
                }
                finally
                {
                    _list.EndUpdate();
                }
            }
            catch (Exception ex)
            {
                Log.Error("PopulateList error: " + ex.Message);
                // Последний fallback: просто очистить и показать что смогли
                try
                {
                    _list.Items.Clear();
                    _list.VirtualMode = false;
                    _virtualMode = false;
                }
                catch { }
            }
        }

        private void OnRetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (_filteredEntries == null || e.ItemIndex < 0 || e.ItemIndex >= _filteredEntries.Count)
            {
                e.Item = new ListViewItem("?");
                return;
            }
            DbpfEntry entry = _filteredEntries[e.ItemIndex];
            string icon;
            if (!string.IsNullOrEmpty(entry.ResolvedName))
                icon = "named";
            else if (entry.IsCompressed)
                icon = "compressed";
            else
                icon = "resource";

            ListViewItem lvi = new ListViewItem(entry.Index.ToString(), icon);
            lvi.SubItems.Add(entry.GetDisplayName());
            lvi.SubItems.Add(entry.GetGroupString());
            lvi.SubItems.Add(entry.GetInstanceString());
            lvi.SubItems.Add(string.Format("0x{0:X8}", entry.Offset));
            lvi.SubItems.Add(Tui.FormatSize(entry.PackedSize));
            lvi.SubItems.Add(Tui.FormatSize(entry.MemSize));
            lvi.SubItems.Add(entry.IsCompressed ? "Yes" : "No");
            lvi.Tag = entry;
            e.Item = lvi;
        }

        private void OnSearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {
            if (_filteredEntries == null) return;
            for (int i = e.StartIndex; i < _filteredEntries.Count; i++)
            {
                if (_filteredEntries[i].GetDisplayName().StartsWith(e.Text, StringComparison.OrdinalIgnoreCase))
                {
                    e.Index = i;
                    return;
                }
            }
        }

        private DbpfEntry GetSelectedEntry()
        {
            if (_list.SelectedItems.Count == 0) return null;
            if (_virtualMode)
            {
                int idx = _list.SelectedItems[0].Index;
                if (idx >= 0 && idx < _filteredEntries.Count)
                    return _filteredEntries[idx];
                return null;
            }
            return _list.SelectedItems[0].Tag as DbpfEntry;
        }

        private void OnTreeSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = e.Node;
            if (node == null) return;

            if (node.Tag is uint)
            {
                uint type = (uint)node.Tag;
                PopulateList(type);
                _statusLabel.Text = "Filtered: " + node.Text + " (" + _list.Items.Count + " entries)";
            }
            else if (node.Tag is string && (string)node.Tag == "root")
            {
                PopulateList(null);
                _statusLabel.Text = _decoder != null ? _decoder.Count + " entries total" : "";
            }
            else if (node.Tag is string)
            {
                string path = (string)node.Tag;
                if (File.Exists(path))
                    OpenPackage(path);
            }
        }

        private void OnListActivate(object sender, EventArgs e)
        {
            OnViewHex(sender, e);
        }

        private void OnClosePackage(object sender, EventArgs e)
        {
            CloseCurrentDecoder();
            _tree.Nodes.Clear();
            _list.Items.Clear();
            _filteredEntries = null;
            _virtualMode = false;
            _list.VirtualMode = false;
            _currentFile = null;
            _statusLabel.Text = "Closed.";
            Tui.SetTitle("Ready");
        }

        private void CloseCurrentDecoder()
        {
            try { if (_decoder != null) _decoder.Dispose(); } catch { }
            _decoder = null;
        }

        private void OnExtractSelected(object sender, EventArgs e)
        {
            if (_decoder == null) return;

            List<DbpfEntry> selected = new List<DbpfEntry>();
            if (_virtualMode)
            {
                foreach (int idx in _list.SelectedIndices)
                {
                    if (idx >= 0 && idx < _filteredEntries.Count)
                        selected.Add(_filteredEntries[idx]);
                }
            }
            else
            {
                foreach (ListViewItem lvi in _list.SelectedItems)
                {
                    DbpfEntry ent = lvi.Tag as DbpfEntry;
                    if (ent != null) selected.Add(ent);
                }
            }

            if (selected.Count == 0) return;

            if (selected.Count == 1)
            {
                DbpfEntry entry = selected[0];
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    string safeName = entry.GetDisplayName()
                        .Replace(" ", "_").Replace("(", "").Replace(")", "")
                        .Replace("/", "_").Replace("\\", "_").Replace(":", "_");
                    sfd.FileName = safeName + "_" + entry.GetInstanceString().Replace(":", "_") + ".bin";
                    if (sfd.ShowDialog(this) != DialogResult.OK) return;
                    try
                    {
                        _progressBar.Visible = true;
                        _progressBar.Style = ProgressBarStyle.Continuous;
                        GuiProgress gp = new GuiProgress(_progressBar, _statusLabel);
                        _decoder.ExtractEntry(entry, sfd.FileName, gp);
                        _statusLabel.Text = "Extracted: " + Path.GetFileName(sfd.FileName);
                        Log.Ok("Extracted: " + Path.GetFileName(sfd.FileName));
                    }
                    catch (Exception ex) { Warn("Extract error: " + ex.Message); }
                    finally { _progressBar.Visible = false; _progressBar.Value = 0; }
                }
            }
            else
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Extract " + selected.Count + " entries to...";
                    if (fbd.ShowDialog(this) != DialogResult.OK) return;
                    try
                    {
                        _progressBar.Visible = true;
                        _progressBar.Style = ProgressBarStyle.Continuous;
                        GuiProgress gp = new GuiProgress(_progressBar, _statusLabel);
                        for (int i = 0; i < selected.Count; i++)
                        {
                            DbpfEntry entry = selected[i];
                            string safeName = entry.GetDisplayName()
                                .Replace(" ", "_").Replace("(", "").Replace(")", "")
                                .Replace("/", "_").Replace("\\", "_").Replace(":", "_");
                            string name = string.Format("{0:D4}_{1}_{2}.bin",
                                entry.Index, safeName, entry.GetInstanceString().Replace(":", "_"));
                            string dest = Path.Combine(fbd.SelectedPath, name);
                            _decoder.ExtractEntry(entry, dest, gp);
                            gp.Report((i + 1) * 100 / selected.Count, i + 1, selected.Count);
                        }
                        _statusLabel.Text = "Extracted " + selected.Count + " entries.";
                        Log.Ok("Extracted " + selected.Count + " entries.");
                    }
                    catch (Exception ex) { Warn("Extract error: " + ex.Message); }
                    finally { _progressBar.Visible = false; _progressBar.Value = 0; }
                }
            }
        }

        private void OnExtractAll(object sender, EventArgs e)
        {
            if (_decoder == null) return;
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Extract ALL entries to...";
                if (fbd.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    _progressBar.Visible = true;
                    _progressBar.Style = ProgressBarStyle.Continuous;
                    GuiProgress gp = new GuiProgress(_progressBar, _statusLabel);
                    for (int i = 0; i < _decoder.Entries.Count; i++)
                    {
                        DbpfEntry entry = _decoder.Entries[i];
                        string safeName = entry.GetDisplayName()
                            .Replace(" ", "_").Replace("(", "").Replace(")", "")
                            .Replace("/", "_").Replace("\\", "_").Replace(":", "_");
                        string name = string.Format("{0:D4}_{1}_{2}.bin",
                            entry.Index, safeName, entry.GetInstanceString().Replace(":", "_"));
                        string dest = Path.Combine(fbd.SelectedPath, name);
                        _decoder.ExtractEntry(entry, dest, gp);
                        gp.Report((i + 1) * 100 / _decoder.Entries.Count, i + 1, _decoder.Entries.Count);
                    }
                    _statusLabel.Text = "Extracted all " + _decoder.Entries.Count + " entries.";
                    Log.Ok("Extract all complete.");
                }
                catch (Exception ex) { Warn("Extract all error: " + ex.Message); }
                finally { _progressBar.Visible = false; _progressBar.Value = 0; }
            }
        }

        private void OnSearchType(object sender, EventArgs e)
        {
            using (InputDialog dlg = new InputDialog("Search by Type",
                "Resource Type (hex, e.g. 0x0333406C):", "0x"))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string input = dlg.InputValue.Trim();
                try
                {
                    uint type;
                    if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        type = uint.Parse(input.Substring(2), NumberStyles.HexNumber);
                    else
                        type = uint.Parse(input, NumberStyles.HexNumber);
                    PopulateList(type);
                    _statusLabel.Text = "Search: 0x" + type.ToString("X8") + " — " +
                        (_filteredEntries != null ? _filteredEntries.Count : 0) + " found";
                }
                catch { Warn("Invalid hex value."); }
            }
        }

        private void OnSearchName(object sender, EventArgs e)
        {
            using (InputDialog dlg = new InputDialog("Search by Name",
                "Resource name (partial match):", ""))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string input = dlg.InputValue.Trim();
                if (string.IsNullOrEmpty(input)) return;
                PopulateList(null, input);
                _statusLabel.Text = "Name search: \"" + input + "\" — " +
                    (_filteredEntries != null ? _filteredEntries.Count : 0) + " found";
            }
        }

        private void OnViewHex(object sender, EventArgs e)
        {
            DbpfEntry entry = GetSelectedEntry();
            if (_decoder == null || entry == null) return;

            byte[] data = _decoder.ReadEntryData(entry);
            if (data == null || data.Length == 0) { Warn("No data."); return; }

            int showLen = Math.Min(256, data.Length);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Entry {0}: {1} | {2}",
                entry.Index, entry.GetDisplayName(), entry.GetInstanceString()));
            sb.AppendLine(string.Format("Type: 0x{0:X8} | Group: 0x{1:X8}",
                entry.ResourceType, entry.ResourceGroup));
            sb.AppendLine(string.Format("Size: {0} bytes | MemSize: {1} | Compressed: {2}",
                data.Length, Tui.FormatSize(entry.MemSize), entry.IsCompressed));
            if (!string.IsNullOrEmpty(entry.ResolvedName))
                sb.AppendLine("Resolved Name: " + entry.ResolvedName);
            sb.AppendLine(new string('-', 72));

            for (int i = 0; i < showLen; i += 16)
            {
                sb.Append(string.Format("{0:X8}  ", i));
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < showLen)
                        sb.Append(string.Format("{0:X2} ", data[i + j]));
                    else
                        sb.Append("   ");
                    if (j == 7) sb.Append(" ");
                }
                sb.Append(" |");
                for (int j = 0; j < 16 && i + j < showLen; j++)
                {
                    byte c = data[i + j];
                    sb.Append(c >= 32 && c < 127 ? (char)c : '.');
                }
                sb.AppendLine("|");
            }

            ShowTextDialog("Hex View — Entry " + entry.Index, sb.ToString());
        }

        private void OnViewText(object sender, EventArgs e)
        {
            DbpfEntry entry = GetSelectedEntry();
            if (_decoder == null || entry == null) return;

            byte[] data = _decoder.ReadEntryData(entry);
            if (data == null || data.Length == 0) { Warn("No data."); return; }

            string text;
            try { text = Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 65536)); }
            catch { text = Encoding.ASCII.GetString(data, 0, Math.Min(data.Length, 65536)); }

            ShowTextDialog("Text View — Entry " + entry.Index + " (" + entry.GetDisplayName() + ")", text);
        }

        private void ShowTextDialog(string title, string content)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = title;
                dlg.Size = new Size(750, 550);
                dlg.StartPosition = FormStartPosition.CenterParent;
                TextBox tb = new TextBox();
                tb.Multiline = true;
                tb.ReadOnly = true;
                tb.ScrollBars = ScrollBars.Both;
                tb.Dock = DockStyle.Fill;
                tb.Font = new Font("Consolas", 9);
                tb.WordWrap = false;
                tb.Text = content;
                dlg.Controls.Add(tb);
                dlg.ShowDialog(this);
            }
        }

        private void OnCopyInstance(object sender, EventArgs e)
        {
            DbpfEntry entry = GetSelectedEntry();
            if (entry == null) return;
            try { Clipboard.SetText(entry.GetInstanceString()); } catch { }
        }

        private void ShowAbout()
        {
            MessageBox.Show(this,
                "EA Package Browser v2.0\n\n" +
                "DBPF archive browser for EA/Maxis games:\n" +
                "• The Sims 2/3/4\n• SimCity 4/2013\n• Spore\n\n" +
                "Supports DBPF v1.0, v1.1, v2.0, v3.0 (partial)\n" +
                "Fault-tolerant: handles corrupted packages\n" +
                "QFS/RefPack decompression\n" +
                "Name resolution: NMAP, STBL, XML Manifest, Spore Locale\n" +
                "No external dependencies.",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Warn(string msg)
        {
            Log.Warn(msg);
            MessageBox.Show(this, msg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_bgWorker.IsBusy)
            {
                _bgWorker.CancelAsync();
                Thread.Sleep(200);
            }
            CloseCurrentDecoder();
            base.OnFormClosing(e);
        }
    }

    internal sealed class InputDialog : Form
    {
        private TextBox _textBox;
        public string InputValue { get { return _textBox.Text.Trim(); } }

        public InputDialog(string title, string label, string defaultValue)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(400, 140);
            ShowInTaskbar = false;

            Label lbl = new Label();
            lbl.Text = label;
            lbl.Location = new Point(12, 15);
            lbl.AutoSize = true;

            _textBox = new TextBox();
            _textBox.Text = defaultValue;
            _textBox.Location = new Point(12, 40);
            _textBox.Width = 360;

            Button ok = new Button();
            ok.Text = "OK";
            ok.DialogResult = DialogResult.OK;
            ok.Location = new Point(200, 78);
            ok.Width = 75;

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Location = new Point(285, 78);
            cancel.Width = 75;

            Controls.Add(lbl);
            Controls.Add(_textBox);
            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }

    // ========================================================================
    //  15. CONSOLE MODE (TUI)
    // ========================================================================
    internal static class ConsoleMode
    {
        public static void Run(string[] args)
        {
            Tui.SetTitle("Console Mode");
            Tui.Header("EA Package Browser — Console Mode");
            Console.WriteLine();

            string action = null;
            string source = null;
            string dest = null;
            bool recursive = false;
            string typeFilter = null;
            string nameFilter = null;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                if (a == "--extract" || a == "-x") action = "extract";
                else if (a == "--list" || a == "-l") action = "list";
                else if (a == "--info" || a == "-n") action = "info";
                else if (a == "--source" || a == "-s") { if (i + 1 < args.Length) source = args[++i]; }
                else if (a == "--dest" || a == "-d") { if (i + 1 < args.Length) dest = args[++i]; }
                else if (a == "--recursive" || a == "-r") recursive = true;
                else if (a == "--type" || a == "-t") { if (i + 1 < args.Length) typeFilter = args[++i]; }
                else if (a == "--name" || a == "-m") { if (i + 1 < args.Length) nameFilter = args[++i]; }
            }

            if (action == null)
            {
                InteractiveMenu();
                return;
            }

            if (source == null)
            {
                Log.Error("No source specified. Use --source <path>");
                return;
            }

            if (recursive && Directory.Exists(source))
            {
                ProcessFolderRecursive(source, action, dest, typeFilter, nameFilter);
                return;
            }

            if (!File.Exists(source))
            {
                Log.Error("File not found: " + source);
                return;
            }

            ProcessFile(source, action, dest, typeFilter, nameFilter);
        }

        private static void ProcessFolderRecursive(string folder, string action, string dest,
            string typeFilter, string nameFilter)
        {
            Log.Info("Scanning folder recursively: " + folder);
            List<string> allFiles = new List<string>();
            try
            {
                allFiles.AddRange(Directory.GetFiles(folder, "*.package", SearchOption.AllDirectories));
                allFiles.AddRange(Directory.GetFiles(folder, "*.world", SearchOption.AllDirectories));
                allFiles.AddRange(Directory.GetFiles(folder, "*.dbc", SearchOption.AllDirectories));
                allFiles.AddRange(Directory.GetFiles(folder, "*.dbpf", SearchOption.AllDirectories));
            }
            catch (Exception ex)
            {
                Log.Error("Folder scan error: " + ex.Message);
                return;
            }

            Log.Info("Found " + allFiles.Count + " package files.");
            ConsoleProgress cp = new ConsoleProgress("Processing");
            for (int i = 0; i < allFiles.Count; i++)
            {
                cp.Report((i + 1) * 100 / allFiles.Count, i + 1, allFiles.Count);
                ProcessFile(allFiles[i], action, dest, typeFilter, nameFilter);
            }
            Console.WriteLine();
            Log.Ok("Batch processing complete.");
        }

        private static void ProcessFile(string path, string action, string dest,
            string typeFilter, string nameFilter)
        {
            DbpfDecoder decoder = new DbpfDecoder(path);
            try
            {
                if (!decoder.Open())
                {
                    Log.Warn("Skipping (cannot open): " + path);
                    return;
                }

                if (action == "info")
                    PrintPackageInfo(decoder);
                else if (action == "list")
                    ListEntries(decoder, typeFilter, nameFilter);
                else if (action == "extract")
                {
                    if (string.IsNullOrEmpty(dest))
                        dest = Path.Combine(Environment.CurrentDirectory, "extracted");
                    ExtractEntries(decoder, dest, typeFilter, nameFilter);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error processing " + path + ": " + ex.Message);
            }
            finally
            {
                decoder.Dispose();
            }
        }

        private static void PrintPackageInfo(DbpfDecoder decoder)
        {
            DbpfHeader h = decoder.Header;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  Package: " + Path.GetFileName(decoder.FilePath));
            Console.ResetColor();
            Console.WriteLine("  Path:        " + decoder.FilePath);
            Console.WriteLine("  Magic:       {0} (0x{1:X8})", h.GetMagicString(), h.Magic);
            Console.WriteLine("  Version:     {0}", h.GetVersionString());
            Console.WriteLine("  Entries:     {0}", h.IndexEntryCount);
            Console.WriteLine("  Index Size:  {0} bytes", h.IndexSize);
            Console.WriteLine("  Index Offset: 0x{0:X8}", h.IsVersion2 ? h.IndexOffset : h.IndexFirstEntry);
            Console.WriteLine("  Encrypted:   {0}", h.IsEncrypted);
            Console.WriteLine("  Created:     {0}", h.GetCreatedDate());
            Console.WriteLine("  Modified:    {0}", h.GetModifiedDate());
            Console.WriteLine("  File Size:   {0}", Tui.FormatSize(new FileInfo(decoder.FilePath).Length));

            // Type summary
            Dictionary<uint, int> typeCounts = new Dictionary<uint, int>();
            int namedCount = 0;
            for (int i = 0; i < decoder.Entries.Count; i++)
            {
                uint t = decoder.Entries[i].ResourceType;
                if (typeCounts.ContainsKey(t)) typeCounts[t]++;
                else typeCounts[t] = 1;
                if (!string.IsNullOrEmpty(decoder.Entries[i].ResolvedName))
                    namedCount++;
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Resource Types ({0} unique):", typeCounts.Count);
            Console.ResetColor();
            foreach (KeyValuePair<uint, int> kv in typeCounts)
            {
                Console.WriteLine("    0x{0:X8} ({1,-25}) x{2}",
                    kv.Key, ResourceTypeNames.GetName(kv.Key), kv.Value);
            }

            if (namedCount > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  Named resources: {0} / {1}", namedCount, decoder.Entries.Count);
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        private static void ListEntries(DbpfDecoder decoder, string typeFilter, string nameFilter)
        {
            uint? filterType = ParseTypeFilter(typeFilter);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  {0,-5} {1,-28} {2,-10} {3,-19} {4,-10} {5,-8} {6,-4}",
                "Idx", "Name / Type", "Group", "Instance", "Offset", "Size", "Cmp");
            Console.WriteLine("  " + new string('-', 90));
            Console.ResetColor();

            int shown = 0;
            for (int i = 0; i < decoder.Entries.Count; i++)
            {
                DbpfEntry e = decoder.Entries[i];
                if (filterType.HasValue && e.ResourceType != filterType.Value) continue;
                if (!string.IsNullOrEmpty(nameFilter))
                {
                    if (e.GetDisplayName().IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                ConsoleColor c;
                if (!string.IsNullOrEmpty(e.ResolvedName))
                    c = ConsoleColor.Green;
                else if (e.IsCompressed)
                    c = ConsoleColor.Yellow;
                else
                    c = ConsoleColor.White;

                Console.ForegroundColor = c;
                Console.WriteLine("  {0,-5} {1,-28} 0x{2:X8} {3,-19} 0x{4:X8} {5,-8} {6,-4}",
                    e.Index,
                    Truncate(e.GetDisplayName(), 28),
                    e.ResourceGroup,
                    e.GetInstanceString(),
                    e.Offset,
                    Tui.FormatSize(e.PackedSize),
                    e.IsCompressed ? "Y" : "N");
                shown++;
            }
            Console.ResetColor();
            Console.WriteLine();
            Log.Info("Listed " + shown + " entries.");
        }

        private static void ExtractEntries(DbpfDecoder decoder, string destDir,
            string typeFilter, string nameFilter)
        {
            uint? filterType = ParseTypeFilter(typeFilter);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            ConsoleProgress cp = new ConsoleProgress("Extract");
            int count = 0;
            int total = decoder.Entries.Count;

            for (int i = 0; i < decoder.Entries.Count; i++)
            {
                DbpfEntry e = decoder.Entries[i];
                if (filterType.HasValue && e.ResourceType != filterType.Value) continue;
                if (!string.IsNullOrEmpty(nameFilter))
                {
                    if (e.GetDisplayName().IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                string safeName = e.GetDisplayName()
                    .Replace(" ", "_").Replace("(", "").Replace(")", "")
                    .Replace("/", "_").Replace("\\", "_").Replace(":", "_")
                    .Replace("*", "").Replace("?", "").Replace("\"", "")
                    .Replace("<", "").Replace(">", "").Replace("|", "");
                string name = string.Format("{0:D4}_{1}_{2}.bin",
                    e.Index, safeName, e.GetInstanceString().Replace(":", "_"));
                string destPath = Path.Combine(destDir, name);

                try
                {
                    decoder.ExtractEntry(e, destPath, null);
                    count++;
                }
                catch (Exception ex)
                {
                    Log.Warn("Extract failed for entry " + e.Index + ": " + ex.Message);
                }
                cp.Report((i + 1) * 100 / total, i + 1, total);
            }
            Console.WriteLine();
            Log.Ok("Extracted " + count + " entries to " + destDir);
        }

        private static uint? ParseTypeFilter(string typeFilter)
        {
            if (string.IsNullOrEmpty(typeFilter)) return null;
            try
            {
                string s = typeFilter.Trim();
                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    s = s.Substring(2);
                return uint.Parse(s, NumberStyles.HexNumber);
            }
            catch { return null; }
        }

        private static string Truncate(string s, int maxLen)
        {
            if (s == null) return "";
            return s.Length <= maxLen ? s : s.Substring(0, maxLen - 2) + "..";
        }

        private static void InteractiveMenu()
        {
            while (true)
            {
                int sel = Tui.ShowMenu("EA Package Browser", new string[]
                {
                    "Open package file",
                    "Scan folder (recursive)",
                    "Package info",
                    "List entries",
                    "Extract all entries",
                    "Exit"
                });

                if (sel < 0 || sel == 5) break;

                try
                {
                    if (sel == 0)
                    {
                        string path = Tui.Prompt("Package file path");
                        if (string.IsNullOrEmpty(path) || !File.Exists(path))
                        {
                            Log.Warn("File not found.");
                            continue;
                        }
                        DbpfDecoder dec = new DbpfDecoder(path);
                        if (dec.Open())
                        {
                            PrintPackageInfo(dec);
                            ListEntries(dec, null, null);
                            dec.Dispose();
                        }
                    }
                    else if (sel == 1)
                    {
                        string folder = Tui.Prompt("Folder path");
                        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                        {
                            Log.Warn("Folder not found.");
                            continue;
                        }
                        ProcessFolderRecursive(folder, "info", null, null, null);
                    }
                    else if (sel == 2)
                    {
                        string path = Tui.Prompt("Package file path");
                        if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                        DbpfDecoder dec = new DbpfDecoder(path);
                        if (dec.Open()) { PrintPackageInfo(dec); dec.Dispose(); }
                    }
                    else if (sel == 3)
                    {
                        string path = Tui.Prompt("Package file path");
                        if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                        string tf = Tui.Prompt("Type filter (hex, empty=all)");
                        string nf = Tui.Prompt("Name filter (empty=all)");
                        DbpfDecoder dec = new DbpfDecoder(path);
                        if (dec.Open()) { ListEntries(dec, tf, nf); dec.Dispose(); }
                    }
                    else if (sel == 4)
                    {
                        string path = Tui.Prompt("Package file path");
                        if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                        string dest = Tui.Prompt("Destination folder");
                        if (string.IsNullOrEmpty(dest))
                            dest = Path.Combine(Environment.CurrentDirectory, "extracted");
                        DbpfDecoder dec = new DbpfDecoder(path);
                        if (dec.Open()) { ExtractEntries(dec, dest, null, null); dec.Dispose(); }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Menu action error: " + ex.Message);
                }

                Console.WriteLine();
                Console.Write("  Press any key to continue...");
                Console.ReadKey(true);
            }
        }
    }

    // ========================================================================
    //  16. PROGRAM ENTRY POINT
    // ========================================================================
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Log.Init();
            Log.Info("=== EA Package Browser v2.0 ===");
            Log.Info("Runtime: " + Environment.Version);
            Log.Info("OS: " + Environment.OSVersion);

            StateManager.Init();
            StateManager.LoadState();

            bool guiMode = false;
            bool consoleMode = false;
            bool showHelp = false;

            foreach (string a in args)
            {
                string la = a.ToLowerInvariant();
                if (la == "--gui" || la == "-g") guiMode = true;
                else if (la == "--console" || la == "-c") consoleMode = true;
                else if (la == "--help" || la == "-h" || la == "/?" || la == "-?") showHelp = true;
            }

            if (showHelp)
            {
                PrintHelp();
                StateManager.Shutdown();
                Log.Shutdown();
                return;
            }

            if (!consoleMode && !guiMode)
            {
                foreach (string a in args)
                {
                    string la = a.ToLowerInvariant();
                    if (la == "--extract" || la == "-x" || la == "--list" || la == "-l" ||
                        la == "--info" || la == "-n")
                    {
                        consoleMode = true;
                        break;
                    }
                }
            }

            if (consoleMode)
            {
                SessionState.ConsoleMode = true;
                ConsoleMode.Run(args);
            }
            else
            {
                SessionState.ConsoleMode = false;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Log.Info("Starting GUI...");
                Tui.SetTitle("GUI");
                Application.Run(new MainForm());
            }

            StateManager.Shutdown();
            Log.Info("Exiting.");
            Log.Shutdown();
        }

        private static void PrintHelp()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("  EA Package Browser v2.0");
            Console.WriteLine("  =======================");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("  USAGE:");
            Console.WriteLine("    EAPackageBrowser.exe [options]");
            Console.WriteLine();
            Console.WriteLine("  MODES:");
            Console.WriteLine("    --gui, -g            Launch WinForms GUI (default)");
            Console.WriteLine("    --console, -c        Launch console TUI mode");
            Console.WriteLine();
            Console.WriteLine("  CONSOLE ACTIONS:");
            Console.WriteLine("    --list, -l           List package entries");
            Console.WriteLine("    --info, -n           Show package header info");
            Console.WriteLine("    --extract, -x        Extract entries from package");
            Console.WriteLine();
            Console.WriteLine("  OPTIONS:");
            Console.WriteLine("    --source, -s <path>  Source package file or folder");
            Console.WriteLine("    --dest, -d <path>    Destination path for extraction");
            Console.WriteLine("    --type, -t <hex>     Filter by resource type (e.g. 0x0333406C)");
            Console.WriteLine("    --name, -m <text>    Filter by resource name (partial match)");
            Console.WriteLine("    --recursive, -r      Process folder recursively");
            Console.WriteLine("    --help, -h, /?       Show this help");
            Console.WriteLine();
            Console.WriteLine("  EXAMPLES:");
            Console.WriteLine("    EAPackageBrowser.exe --gui");
            Console.WriteLine("    EAPackageBrowser.exe -c -n -s \"C:\\Game\\FullBuild0.package\"");
            Console.WriteLine("    EAPackageBrowser.exe -c -l -s test.package -t 0x0333406C");
            Console.WriteLine("    EAPackageBrowser.exe -c -l -s test.package -m \"XML\"");
            Console.WriteLine("    EAPackageBrowser.exe -c -x -s test.package -d C:\\output");
            Console.WriteLine("    EAPackageBrowser.exe -c -l -r -s \"C:\\Game\\Packages\"");
            Console.WriteLine();
            Console.WriteLine("  SUPPORTED FORMATS:");
            Console.WriteLine("    *.package  General DBPF resource collection");
            Console.WriteLine("    *.world    World resources (Sims 3)");
            Console.WriteLine("    *.dbc      DBPF archive collection");
            Console.WriteLine("    *.dbpf     Generic DBPF archive");
            Console.WriteLine();
            Console.WriteLine("  SUPPORTED GAMES:");
            Console.WriteLine("    The Sims 2, The Sims 3, The Sims 4");
            Console.WriteLine("    SimCity 4, SimCity 2013");
            Console.WriteLine("    Spore, MySims");
            Console.WriteLine();
            Console.WriteLine("  DBPF VERSIONS: 1.0, 1.1, 2.0, 3.0 (partial)");
            Console.WriteLine("  COMPRESSION: QFS/RefPack (0x10FB, 0xFB10, 0xFB50)");
            Console.WriteLine("  NAME RESOLUTION: NMAP, STBL, XML Manifest, Spore Locale");
            Console.WriteLine();
        }
    }
}