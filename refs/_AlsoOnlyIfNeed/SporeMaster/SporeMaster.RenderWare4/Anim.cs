using System.IO;
using Gibbed.Spore.Helpers;

namespace SporeMaster.RenderWare4;

internal class Anim : RW4Object
{
	public const int type_code = 458753;

	public uint skeleton_id;

	public float length;

	public uint flags;

	public uint[] channel_names;

	public JointPose[,] channel_frame_pose;

	public uint padding;

	public override void Read(RW4Model m, RW4Section s, Stream r)
	{
		if (s.type_code != 458753)
		{
			throw new ModelFormatException(r, "AN000", s.type_code);
		}
		uint p1 = r.ReadU32();
		uint channels = r.ReadU32();
		skeleton_id = r.ReadU32();
		r.expect(0u, "AN001");
		uint p3 = r.ReadU32();
		uint p4 = r.ReadU32();
		r.expect(channels, "AN002");
		r.expect(0u, "AN003");
		length = r.ReadF32();
		r.expect(12u, "AN010");
		flags = r.ReadU32();
		uint p5 = r.ReadU32();
		if (p1 != r.Position)
		{
			throw new ModelFormatException(r, "AN100", p1);
		}
		if (p5 != p1 + channels * 4)
		{
			throw new ModelFormatException(r, "AN101", p5);
		}
		if (p3 != p5 + channels * 12)
		{
			throw new ModelFormatException(r, "AN102", p3);
		}
		uint keyframe_count = 0u;
		channel_names = new uint[channels];
		for (int i = 0; i < channels; i++)
		{
			channel_names[i] = r.ReadU32();
		}
		for (int j = 0; j < channels; j++)
		{
			uint p6 = r.ReadU32() - (48 + channels * 4 + channels * 12);
			uint pose_size = r.ReadU32();
			uint pose_components = r.ReadU32();
			if (pose_components != 1537 || pose_size != 48)
			{
				throw new ModelFormatException(r, "AN200 Pose format not supported", pose_components);
			}
			if (j == 1)
			{
				keyframe_count = p6 / 48;
			}
			else if (j >= 1 && p6 != (long)j * 48L * keyframe_count)
			{
				throw new ModelFormatException(r, "AN201", null);
			}
		}
		if (channels == 1)
		{
			keyframe_count = (p4 - p3) / (channels * 48);
		}
		channel_frame_pose = new JointPose[channels, keyframe_count];
		for (int c = 0; c < channels; c++)
		{
			for (int f = 0; f < keyframe_count; f++)
			{
				channel_frame_pose[c, f].Read(r);
				if (channels != 1 || channel_frame_pose[c, f].time != length)
				{
					continue;
				}
				keyframe_count = (uint)(f + 1);
				JointPose[,] t = new JointPose[channels, keyframe_count];
				for (int k = 0; k < channels; k++)
				{
					for (int l = 0; l < keyframe_count; l++)
					{
						ref JointPose reference = ref t[k, l];
						reference = channel_frame_pose[k, l];
					}
				}
				channel_frame_pose = t;
				break;
			}
		}
		padding = p4 - p3 - channels * keyframe_count * 48;
		r.ReadPadding(padding);
	}

	public override void Write(RW4Model m, RW4Section s, Stream w)
	{
		uint channels = (uint)channel_names.Length;
		uint keyframe_count = (uint)channel_frame_pose.GetLength(1);
		uint p1 = (uint)((int)w.Position + 48);
		w.WriteU32(p1);
		w.WriteU32(channels);
		w.WriteU32(skeleton_id);
		w.WriteU32(0u);
		w.WriteU32(p1 + channels * 4 + channels * 12);
		w.WriteU32(p1 + channels * 4 + channels * 12 + channels * keyframe_count * 48 + padding);
		w.WriteU32(channels);
		w.WriteU32(0u);
		w.WriteF32(length);
		w.WriteU32(12u);
		w.WriteU32(flags);
		w.WriteU32(p1 + channels * 4);
		w.WriteU32s(channel_names);
		for (int i = 0; i < channels; i++)
		{
			w.WriteU32((uint)(48 + channels * 4 + channels * 12 + (long)i * 48L * keyframe_count));
			w.WriteU32(48u);
			w.WriteU32(1537u);
		}
		for (int c = 0; c < channels; c++)
		{
			for (int f = 0; f < keyframe_count; f++)
			{
				channel_frame_pose[c, f].Write(w);
			}
		}
		w.WritePadding(padding);
	}

	public override int ComputeSize()
	{
		int channels = channel_names.Length;
		int keyframe_count = channel_frame_pose.GetLength(1);
		return (int)(48 + channels * 4 + channels * 12 + (long)(channels * keyframe_count) * 48L + padding);
	}
}
