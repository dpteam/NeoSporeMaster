using System.Globalization;

namespace SporeMaster.Core.RenderWare4;

/// <summary>How the integer value of a Direct3D state should be rendered.</summary>
public enum D3DValueKind
{
    Int,
    Float,
    ColorRgba,
    Boolean,
    ZwBufferType,
    FillMode,
    ShadeMode,
    Blend,
    Cull,
    CmpFunc,
    FogMode,
    StencilOp,
    WrapCoord,
    MaterialColorSource,
    VertexBlendFlags,
    PatchEdgeStyle,
    DebugMonitorTokens,
    BlendOp,
    DegreeType,
    PrimitiveType,
    TextureOp,
    TextureAddress,
    TextureFilterType,
    TextureTransformFlags,
    TextureArg,
}

/// <summary>
/// A human-readable description of a Direct3D render state, texture-stage state or sampler state:
/// its name and how its value should be interpreted.
/// </summary>
public readonly record struct D3DStateInfo(string Name, D3DValueKind Kind);

/// <summary>
/// Direct3D state tables used by RenderWare 4 compiled material states (ported from SporeModder FX's
/// <c>Direct3DEnums</c>). Provides names for render/stage/sampler states and formats their values
/// (as integers, floats, packed colours or enum names).
/// </summary>
public static class Direct3DStates
{
    private static readonly Dictionary<int, D3DStateInfo> RenderStateTable = new()
    {
        [7] = new("D3DRS_ZENABLE", D3DValueKind.ZwBufferType),
        [8] = new("D3DRS_FILLMODE", D3DValueKind.FillMode),
        [9] = new("D3DRS_SHADEMODE", D3DValueKind.ShadeMode),
        [14] = new("D3DRS_ZWRITEENABLE", D3DValueKind.Boolean),
        [15] = new("D3DRS_ALPHATESTENABLE", D3DValueKind.Boolean),
        [16] = new("D3DRS_LASTPIXEL", D3DValueKind.Boolean),
        [19] = new("D3DRS_SRCBLEND", D3DValueKind.Blend),
        [20] = new("D3DRS_DESTBLEND", D3DValueKind.Blend),
        [22] = new("D3DRS_CULLMODE", D3DValueKind.Cull),
        [23] = new("D3DRS_ZFUNC", D3DValueKind.CmpFunc),
        [24] = new("D3DRS_ALPHAREF", D3DValueKind.Int),
        [25] = new("D3DRS_ALPHAFUNC", D3DValueKind.CmpFunc),
        [26] = new("D3DRS_DITHERENABLE", D3DValueKind.Boolean),
        [27] = new("D3DRS_ALPHABLENDENABLE", D3DValueKind.Boolean),
        [28] = new("D3DRS_FOGENABLE", D3DValueKind.Boolean),
        [29] = new("D3DRS_SPECULARENABLE", D3DValueKind.Boolean),
        [34] = new("D3DRS_FOGCOLOR", D3DValueKind.ColorRgba),
        [35] = new("D3DRS_FOGTABLEMODE", D3DValueKind.FogMode),
        [36] = new("D3DRS_FOGSTART", D3DValueKind.Float),
        [37] = new("D3DRS_FOGEND", D3DValueKind.Float),
        [38] = new("D3DRS_FOGDENSITY", D3DValueKind.Float),
        [48] = new("D3DRS_RANGEFOGENABLE", D3DValueKind.Boolean),
        [52] = new("D3DRS_STENCILENABLE", D3DValueKind.Boolean),
        [53] = new("D3DRS_STENCILFAIL", D3DValueKind.StencilOp),
        [54] = new("D3DRS_STENCILZFAIL", D3DValueKind.StencilOp),
        [55] = new("D3DRS_STENCILPASS", D3DValueKind.StencilOp),
        [56] = new("D3DRS_STENCILFUNC", D3DValueKind.CmpFunc),
        [57] = new("D3DRS_STENCILREF", D3DValueKind.Int),
        [58] = new("D3DRS_STENCILMASK", D3DValueKind.Int),
        [59] = new("D3DRS_STENCILWRITEMASK", D3DValueKind.Int),
        [60] = new("D3DRS_TEXTUREFACTOR", D3DValueKind.ColorRgba),
        [128] = new("D3DRS_WRAP0", D3DValueKind.WrapCoord),
        [129] = new("D3DRS_WRAP1", D3DValueKind.WrapCoord),
        [130] = new("D3DRS_WRAP2", D3DValueKind.WrapCoord),
        [131] = new("D3DRS_WRAP3", D3DValueKind.WrapCoord),
        [132] = new("D3DRS_WRAP4", D3DValueKind.WrapCoord),
        [133] = new("D3DRS_WRAP5", D3DValueKind.WrapCoord),
        [134] = new("D3DRS_WRAP6", D3DValueKind.WrapCoord),
        [135] = new("D3DRS_WRAP7", D3DValueKind.WrapCoord),
        [136] = new("D3DRS_CLIPPING", D3DValueKind.Boolean),
        [137] = new("D3DRS_LIGHTING", D3DValueKind.Boolean),
        [139] = new("D3DRS_AMBIENT", D3DValueKind.ColorRgba),
        [140] = new("D3DRS_FOGVERTEXMODE", D3DValueKind.FogMode),
        [141] = new("D3DRS_COLORVERTEX", D3DValueKind.Boolean),
        [142] = new("D3DRS_LOCALVIEWER", D3DValueKind.Boolean),
        [143] = new("D3DRS_NORMALIZENORMALS", D3DValueKind.Boolean),
        [145] = new("D3DRS_DIFFUSEMATERIALSOURCE", D3DValueKind.MaterialColorSource),
        [146] = new("D3DRS_SPECULARMATERIALSOURCE", D3DValueKind.MaterialColorSource),
        [147] = new("D3DRS_AMBIENTMATERIALSOURCE", D3DValueKind.MaterialColorSource),
        [148] = new("D3DRS_EMISSIVEMATERIALSOURCE", D3DValueKind.MaterialColorSource),
        [151] = new("D3DRS_VERTEXBLEND", D3DValueKind.VertexBlendFlags),
        [152] = new("D3DRS_CLIPPLANEENABLE", D3DValueKind.Int),
        [154] = new("D3DRS_POINTSIZE", D3DValueKind.Float),
        [155] = new("D3DRS_POINTSIZE_MIN", D3DValueKind.Float),
        [156] = new("D3DRS_POINTSPRITEENABLE", D3DValueKind.Boolean),
        [157] = new("D3DRS_POINTSCALEENABLE", D3DValueKind.Boolean),
        [158] = new("D3DRS_POINTSCALE_A", D3DValueKind.Float),
        [159] = new("D3DRS_POINTSCALE_B", D3DValueKind.Float),
        [160] = new("D3DRS_POINTSCALE_C", D3DValueKind.Float),
        [161] = new("D3DRS_MULTISAMPLEANTIALIAS", D3DValueKind.Boolean),
        [162] = new("D3DRS_MULTISAMPLEMASK", D3DValueKind.Int),
        [163] = new("D3DRS_PATCHEDGESTYLE", D3DValueKind.PatchEdgeStyle),
        [165] = new("D3DRS_DEBUGMONITORTOKEN", D3DValueKind.DebugMonitorTokens),
        [166] = new("D3DRS_POINTSIZE_MAX", D3DValueKind.Float),
        [167] = new("D3DRS_INDEXEDVERTEXBLENDENABLE", D3DValueKind.Boolean),
        [168] = new("D3DRS_COLORWRITEENABLE", D3DValueKind.Int),
        [170] = new("D3DRS_TWEENFACTOR", D3DValueKind.Float),
        [171] = new("D3DRS_BLENDOP", D3DValueKind.BlendOp),
        [172] = new("D3DRS_POSITIONDEGREE", D3DValueKind.DegreeType),
        [173] = new("D3DRS_NORMALDEGREE", D3DValueKind.DegreeType),
        [174] = new("D3DRS_SCISSORTESTENABLE", D3DValueKind.Boolean),
        [175] = new("D3DRS_SLOPESCALEDEPTHBIAS", D3DValueKind.Float),
        [176] = new("D3DRS_ANTIALIASEDLINEENABLE", D3DValueKind.Boolean),
        [178] = new("D3DRS_MINTESSELLATIONLEVEL", D3DValueKind.Float),
        [179] = new("D3DRS_MAXTESSELLATIONLEVEL", D3DValueKind.Float),
        [180] = new("D3DRS_ADAPTIVETESS_X", D3DValueKind.Float),
        [181] = new("D3DRS_ADAPTIVETESS_Y", D3DValueKind.Float),
        [182] = new("D3DRS_ADAPTIVETESS_Z", D3DValueKind.Float),
        [183] = new("D3DRS_ADAPTIVETESS_W", D3DValueKind.Float),
        [184] = new("D3DRS_ENABLEADAPTIVETESSELLATION", D3DValueKind.Boolean),
        [185] = new("D3DRS_TWOSIDEDSTENCILMODE", D3DValueKind.Boolean),
        [186] = new("D3DRS_CCW_STENCILFAIL", D3DValueKind.StencilOp),
        [187] = new("D3DRS_CCW_STENCILZFAIL", D3DValueKind.StencilOp),
        [188] = new("D3DRS_CCW_STENCILPASS", D3DValueKind.StencilOp),
        [189] = new("D3DRS_CCW_STENCILFUNC", D3DValueKind.CmpFunc),
        [190] = new("D3DRS_COLORWRITEENABLE1", D3DValueKind.Int),
        [191] = new("D3DRS_COLORWRITEENABLE2", D3DValueKind.Int),
        [192] = new("D3DRS_COLORWRITEENABLE3", D3DValueKind.Int),
        [193] = new("D3DRS_BLENDFACTOR", D3DValueKind.ColorRgba),
        [194] = new("D3DRS_SRGBWRITEENABLE", D3DValueKind.Int),
        [195] = new("D3DRS_DEPTHBIAS", D3DValueKind.Float),
        [198] = new("D3DRS_WRAP8", D3DValueKind.WrapCoord),
        [199] = new("D3DRS_WRAP9", D3DValueKind.WrapCoord),
        [200] = new("D3DRS_WRAP10", D3DValueKind.WrapCoord),
        [201] = new("D3DRS_WRAP11", D3DValueKind.WrapCoord),
        [202] = new("D3DRS_WRAP12", D3DValueKind.WrapCoord),
        [203] = new("D3DRS_WRAP13", D3DValueKind.WrapCoord),
        [204] = new("D3DRS_WRAP14", D3DValueKind.WrapCoord),
        [205] = new("D3DRS_WRAP15", D3DValueKind.WrapCoord),
        [206] = new("D3DRS_SEPARATEALPHABLENDENABLE", D3DValueKind.Boolean),
        [207] = new("D3DRS_SRCBLENDALPHA", D3DValueKind.Blend),
        [208] = new("D3DRS_DESTBLENDALPHA", D3DValueKind.Blend),
        [209] = new("D3DRS_BLENDOPALPHA", D3DValueKind.BlendOp),
    };

    private static readonly Dictionary<int, D3DStateInfo> TextureStageStateTable = new()
    {
        [1] = new("D3DTSS_COLOROP", D3DValueKind.TextureOp),
        [2] = new("D3DTSS_COLORARG1", D3DValueKind.TextureArg),
        [3] = new("D3DTSS_COLORARG2", D3DValueKind.TextureArg),
        [4] = new("D3DTSS_ALPHAOP", D3DValueKind.TextureOp),
        [5] = new("D3DTSS_ALPHAARG1", D3DValueKind.TextureArg),
        [6] = new("D3DTSS_ALPHAARG2", D3DValueKind.TextureArg),
        [7] = new("D3DTSS_BUMPENVMAT00", D3DValueKind.Float),
        [8] = new("D3DTSS_BUMPENVMAT01", D3DValueKind.Float),
        [9] = new("D3DTSS_BUMPENVMAT10", D3DValueKind.Float),
        [10] = new("D3DTSS_BUMPENVMAT11", D3DValueKind.Float),
        [11] = new("D3DTSS_TEXCOORDINDEX", D3DValueKind.Int),
        [22] = new("D3DTSS_BUMPENVLSCALE", D3DValueKind.Float),
        [23] = new("D3DTSS_BUMPENVLOFFSET", D3DValueKind.Float),
        [24] = new("D3DTSS_TEXTURETRANSFORMFLAGS", D3DValueKind.TextureTransformFlags),
        [26] = new("D3DTSS_COLORARG0", D3DValueKind.TextureArg),
        [27] = new("D3DTSS_ALPHAARG0", D3DValueKind.TextureArg),
        [28] = new("D3DTSS_RESULTARG", D3DValueKind.TextureArg),
        [32] = new("D3DTSS_CONSTANT", D3DValueKind.TextureArg),
    };

    private static readonly Dictionary<int, D3DStateInfo> SamplerStateTable = new()
    {
        [1] = new("D3DSAMP_ADDRESSU", D3DValueKind.TextureAddress),
        [2] = new("D3DSAMP_ADDRESSV", D3DValueKind.TextureAddress),
        [3] = new("D3DSAMP_ADDRESSW", D3DValueKind.TextureAddress),
        [4] = new("D3DSAMP_BORDERCOLOR", D3DValueKind.ColorRgba),
        [5] = new("D3DSAMP_MAGFILTER", D3DValueKind.TextureFilterType),
        [6] = new("D3DSAMP_MINFILTER", D3DValueKind.TextureFilterType),
        [7] = new("D3DSAMP_MIPFILTER", D3DValueKind.TextureFilterType),
        [8] = new("D3DSAMP_MIPMAPLODBIAS", D3DValueKind.Int),
        [9] = new("D3DSAMP_MAXMIPLEVEL", D3DValueKind.Int),
        [10] = new("D3DSAMP_MAXANISOTROPY", D3DValueKind.Int),
        [11] = new("D3DSAMP_SRGBTEXTURE", D3DValueKind.Int),
        [12] = new("D3DSAMP_ELEMENTINDEX", D3DValueKind.Int),
        [13] = new("D3DSAMP_DMAPOFFSET", D3DValueKind.Int),
    };

    private static readonly Dictionary<D3DValueKind, Dictionary<int, string>> EnumNames = new()
    {
        [D3DValueKind.Boolean] = Names((0, "FALSE"), (1, "TRUE")),
        [D3DValueKind.ZwBufferType] = Names((0, "D3DZB_FALSE"), (1, "D3DZB_TRUE"), (2, "D3DZB_USEW")),
        [D3DValueKind.FillMode] = Names((1, "D3DFILL_POINT"), (2, "D3DFILL_WIREFRAME"), (3, "D3DFILL_SOLID")),
        [D3DValueKind.ShadeMode] = Names((1, "D3DSHADE_FLAT"), (2, "D3DSHADE_GOURAUD"), (3, "D3DSHADE_PHONG")),
        [D3DValueKind.Blend] = Names(
            (1, "D3DBLEND_ZERO"), (2, "D3DBLEND_ONE"), (3, "D3DBLEND_SRCCOLOR"),
            (4, "D3DBLEND_INVSRCCOLOR"), (5, "D3DBLEND_SRCALPHA"), (6, "D3DBLEND_INVSRCALPHA"),
            (7, "D3DBLEND_DESTALPHA"), (8, "D3DBLEND_INVDESTALPHA"), (9, "D3DBLEND_DESTCOLOR"),
            (10, "D3DBLEND_INVDESTCOLOR"), (11, "D3DBLEND_SRCALPHASAT"), (12, "D3DBLEND_BOTHSRCALPHA"),
            (13, "D3DBLEND_BOTHINVSRCALPHA"), (14, "D3DBLEND_BLENDFACTOR"), (15, "D3DBLEND_INVBLENDFACTOR"),
            (16, "D3DBLEND_SRCCOLOR2"), (17, "D3DBLEND_INVSRCCOLOR2")),
        [D3DValueKind.Cull] = Names((1, "D3DCULL_NONE"), (2, "D3DCULL_CW"), (3, "D3DCULL_CCW")),
        [D3DValueKind.CmpFunc] = Names(
            (1, "D3DCMP_NEVER"), (2, "D3DCMP_LESS"), (3, "D3DCMP_EQUAL"), (4, "D3DCMP_LESSEQUAL"),
            (5, "D3DCMP_GREATER"), (6, "D3DCMP_NOTEQUAL"), (7, "D3DCMP_GREATEREQUAL"), (8, "D3DCMP_ALWAYS")),
        [D3DValueKind.FogMode] = Names(
            (0, "D3DFOG_NONE"), (1, "D3DFOG_EXP"), (2, "D3DFOG_EXP2"), (3, "D3DFOG_LINEAR")),
        [D3DValueKind.StencilOp] = Names(
            (1, "D3DSTENCILOP_KEEP"), (2, "D3DSTENCILOP_ZERO"), (3, "D3DSTENCILOP_REPLACE"),
            (4, "D3DSTENCILOP_INCRSAT"), (5, "D3DSTENCILOP_DECRSAT"), (6, "D3DSTENCILOP_INVERT"),
            (7, "D3DSTENCILOP_INCR"), (8, "D3DSTENCILOP_DECR")),
        [D3DValueKind.WrapCoord] = Names((0, "NONE"), (1, "D3DWRAPCOORD_0"), (2, "D3DWRAPCOORD_1"), (4, "D3DWRAPCOORD_2"), (8, "D3DWRAPCOORD_3")),
        [D3DValueKind.MaterialColorSource] = Names((0, "D3DMCS_MATERIAL"), (1, "D3DMCS_COLOR1"), (2, "D3DMCS_COLOR2")),
        [D3DValueKind.VertexBlendFlags] = Names(
            (0, "D3DVBF_DISABLE"), (1, "D3DVBF_1WEIGHTS"), (2, "D3DVBF_2WEIGHTS"),
            (3, "D3DVBF_3WEIGHTS"), (255, "D3DVBF_TWEENING"), (256, "D3DVBF_0WEIGHTS")),
        [D3DValueKind.PatchEdgeStyle] = Names((0, "D3DPATCHEDGE_DISCRETE"), (1, "D3DPATCHEDGE_CONTINUOUS")),
        [D3DValueKind.DebugMonitorTokens] = Names((0, "D3DDMT_ENABLE"), (1, "D3DDMT_DISABLE")),
        [D3DValueKind.BlendOp] = Names(
            (1, "D3DBLENDOP_ADD"), (2, "D3DBLENDOP_SUBTRACT"), (3, "D3DBLENDOP_REVSUBTRACT"),
            (4, "D3DBLENDOP_MIN"), (5, "D3DBLENDOP_MAX")),
        [D3DValueKind.DegreeType] = Names(
            (1, "D3DDEGREE_LINEAR"), (2, "D3DDEGREE_QUADRATIC"), (3, "D3DDEGREE_CUBIC"), (5, "D3DDEGREE_QUINTIC")),
        [D3DValueKind.PrimitiveType] = Names(
            (1, "D3DPT_POINTLIST"), (2, "D3DPT_LINELIST"), (3, "D3DPT_LINESTRIP"),
            (4, "D3DPT_TRIANGLELIST"), (5, "D3DPT_TRIANGLESTRIP"), (6, "D3DPT_TRIANGLEFAN")),
        [D3DValueKind.TextureOp] = Names(
            (1, "D3DTOP_DISABLE"), (2, "D3DTOP_SELECTARG1"), (3, "D3DTOP_SELECTARG2"), (4, "D3DTOP_MODULATE"),
            (5, "D3DTOP_MODULATE2X"), (6, "D3DTOP_MODULATE4X"), (7, "D3DTOP_ADD"), (8, "D3DTOP_ADDSIGNED"),
            (9, "D3DTOP_ADDSIGNED2X"), (10, "D3DTOP_SUBTRACT"), (11, "D3DTOP_ADDSMOOTH"),
            (12, "D3DTOP_BLENDDIFFUSEALPHA"), (13, "D3DTOP_BLENDTEXTUREALPHA"), (14, "D3DTOP_BLENDFACTORALPHA"),
            (15, "D3DTOP_BLENDTEXTUREALPHAPM"), (16, "D3DTOP_BLENDCURRENTALPHA"), (17, "D3DTOP_PREMODULATE"),
            (18, "D3DTOP_MODULATEALPHA_ADDCOLOR"), (19, "D3DTOP_MODULATECOLOR_ADDALPHA"),
            (20, "D3DTOP_MODULATEINVALPHA_ADDCOLOR"), (21, "D3DTOP_MODULATEINVCOLOR_ADDALPHA"),
            (22, "D3DTOP_BUMPENVMAP"), (23, "D3DTOP_BUMPENVMAPLUMINANCE"), (24, "D3DTOP_DOTPRODUCT3"),
            (25, "D3DTOP_MULTIPLYADD"), (26, "D3DTOP_LERP")),
        [D3DValueKind.TextureAddress] = Names(
            (1, "D3DTADDRESS_WRAP"), (2, "D3DTADDRESS_MIRROR"), (3, "D3DTADDRESS_CLAMP"),
            (4, "D3DTADDRESS_BORDER"), (5, "D3DTADDRESS_MIRRORONCE")),
        [D3DValueKind.TextureFilterType] = Names(
            (0, "D3DTEXF_NONE"), (1, "D3DTEXF_POINT"), (2, "D3DTEXF_LINEAR"), (3, "D3DTEXF_ANISOTROPIC"),
            (6, "D3DTEXF_PYRAMIDALQUAD"), (7, "D3DTEXF_GAUSSIANQUAD"), (8, "D3DTEXF_CONVOLUTIONMONO")),
        [D3DValueKind.TextureTransformFlags] = Names(
            (0, "D3DTTFF_DISABLE"), (1, "D3DTTFF_COUNT1"), (2, "D3DTTFF_COUNT2"), (3, "D3DTTFF_COUNT3"),
            (4, "D3DTTFF_COUNT4"), (256, "D3DTTFF_PROJECTED")),
        [D3DValueKind.TextureArg] = Names(
            (0x0, "D3DTA_DIFFUSE"), (0x1, "D3DTA_CURRENT"), (0x2, "D3DTA_TEXTURE"), (0x3, "D3DTA_TFACTOR"),
            (0x4, "D3DTA_SPECULAR"), (0x5, "D3DTA_TEMP"), (0x6, "D3DTA_CONSTANT"),
            (0x10, "D3DTA_COMPLEMENT"), (0x20, "D3DTA_ALPHAREPLICATE")),
    };

    private static Dictionary<int, string> Names(params (int Value, string Name)[] entries)
    {
        var map = new Dictionary<int, string>();
        foreach ((int value, string name) in entries)
        {
            map[value] = name;
        }

        return map;
    }

    public static D3DStateInfo DescribeRenderState(int id) =>
        RenderStateTable.TryGetValue(id, out D3DStateInfo info) ? info : new D3DStateInfo($"0x{id:X}", D3DValueKind.Int);

    public static D3DStateInfo DescribeTextureStageState(int id) =>
        TextureStageStateTable.TryGetValue(id, out D3DStateInfo info) ? info : new D3DStateInfo($"0x{id:X}", D3DValueKind.Int);

    public static D3DStateInfo DescribeSamplerState(int id) =>
        SamplerStateTable.TryGetValue(id, out D3DStateInfo info) ? info : new D3DStateInfo($"0x{id:X}", D3DValueKind.Int);

    /// <summary>Formats the raw integer value of a state according to its <see cref="D3DValueKind"/>.</summary>
    public static string FormatValue(D3DValueKind kind, int value)
    {
        switch (kind)
        {
            case D3DValueKind.Int:
                return value.ToString(CultureInfo.InvariantCulture);
            case D3DValueKind.Float:
                return BitConverter.Int32BitsToSingle(value).ToString("0.######", CultureInfo.InvariantCulture);
            case D3DValueKind.ColorRgba:
                return FormatColor(value);
            default:
                if (EnumNames.TryGetValue(kind, out Dictionary<int, string>? table) && table.TryGetValue(value, out string? name))
                {
                    return name;
                }

                return value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Formats a packed ARGB colour the way SporeModder FX renders it: <c>(r, g, b, a)</c> in 0..1.</summary>
    private static string FormatColor(int code)
    {
        float a = ((code >> 24) & 0xFF) / 255.0f;
        float r = ((code >> 16) & 0xFF) / 255.0f;
        float g = ((code >> 8) & 0xFF) / 255.0f;
        float b = (code & 0xFF) / 255.0f;
        return string.Format(
            CultureInfo.InvariantCulture,
            "({0:0.###}, {1:0.###}, {2:0.###}, {3:0.###})",
            r,
            g,
            b,
            a);
    }

    /// <summary>Formats a primitive type id (from a compiled material header).</summary>
    public static string FormatPrimitiveType(int id) => FormatValue(D3DValueKind.PrimitiveType, id);
}
