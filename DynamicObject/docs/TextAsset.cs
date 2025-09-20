using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine
{
	// Token: 0x0200025D RID: 605
	[NativeHeader("Runtime/Scripting/TextAsset.h")]
	public class TextAsset : Object
	{
		// Token: 0x170004DC RID: 1244
		// (get) Token: 0x06001983 RID: 6531
		public extern byte[] bytes
		{
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		// Token: 0x06001984 RID: 6532
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern byte[] GetPreviewBytes(int maxByteCount);

		// Token: 0x06001985 RID: 6533
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void Internal_CreateInstance([Writable] TextAsset self, string text);

		// Token: 0x06001986 RID: 6534
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern IntPtr GetDataPtr();

		// Token: 0x06001987 RID: 6535
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern long GetDataSize();

		// Token: 0x170004DD RID: 1245
		// (get) Token: 0x06001988 RID: 6536 RVA: 0x0002AD90 File Offset: 0x00028F90
		public string text
		{
			get
			{
				byte[] bytes = this.bytes;
				return (bytes.Length == 0) ? string.Empty : TextAsset.DecodeString(bytes);
			}
		}

		// Token: 0x170004DE RID: 1246
		// (get) Token: 0x06001989 RID: 6537 RVA: 0x0002ADBA File Offset: 0x00028FBA
		public long dataSize
		{
			get
			{
				return this.GetDataSize();
			}
		}

		// Token: 0x0600198A RID: 6538 RVA: 0x0002ADC4 File Offset: 0x00028FC4
		public override string ToString()
		{
			return this.text;
		}

		// Token: 0x0600198B RID: 6539 RVA: 0x0002ADDC File Offset: 0x00028FDC
		public TextAsset()
			: this(TextAsset.CreateOptions.CreateNativeObject, null)
		{
		}

		// Token: 0x0600198C RID: 6540 RVA: 0x0002ADE8 File Offset: 0x00028FE8
		public TextAsset(string text)
			: this(TextAsset.CreateOptions.CreateNativeObject, text)
		{
		}

		// Token: 0x0600198D RID: 6541 RVA: 0x0002ADF4 File Offset: 0x00028FF4
		internal TextAsset(TextAsset.CreateOptions options, string text)
		{
			bool flag = options == TextAsset.CreateOptions.CreateNativeObject;
			if (flag)
			{
				TextAsset.Internal_CreateInstance(this, text);
			}
		}

		// Token: 0x0600198E RID: 6542 RVA: 0x0002AE1C File Offset: 0x0002901C
		public unsafe NativeArray<T> GetData<T>() where T : struct
		{
			long dataSize = this.GetDataSize();
			long num = (long)UnsafeUtility.SizeOf<T>();
			bool flag = dataSize % num != 0L;
			if (flag)
			{
				throw new ArgumentException(string.Format("Type passed to {0} can't capture the asset data. Data size is {1} which is not a multiple of type size {2}", "GetData", dataSize, num));
			}
			long num2 = dataSize / num;
			return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)this.GetDataPtr(), (int)num2, Allocator.None);
		}

		// Token: 0x0600198F RID: 6543 RVA: 0x0002AE88 File Offset: 0x00029088
		internal string GetPreview(int maxChars)
		{
			return TextAsset.DecodeString(this.GetPreviewBytes(maxChars * 4));
		}

		// Token: 0x06001990 RID: 6544 RVA: 0x0002AEA8 File Offset: 0x000290A8
		internal static string DecodeString(byte[] bytes)
		{
			int num = TextAsset.EncodingUtility.encodingLookup.Length;
			int i = 0;
			int num2;
			while (i < num)
			{
				byte[] key = TextAsset.EncodingUtility.encodingLookup[i].Key;
				num2 = key.Length;
				bool flag = bytes.Length >= num2;
				if (flag)
				{
					for (int j = 0; j < num2; j++)
					{
						bool flag2 = key[j] != bytes[j];
						if (flag2)
						{
							num2 = -1;
						}
					}
					bool flag3 = num2 < 0;
					if (!flag3)
					{
						try
						{
							Encoding value = TextAsset.EncodingUtility.encodingLookup[i].Value;
							return value.GetString(bytes, num2, bytes.Length - num2);
						}
						catch
						{
						}
					}
				}
				IL_00A2:
				i++;
				continue;
				goto IL_00A2;
			}
			num2 = 0;
			Encoding targetEncoding = TextAsset.EncodingUtility.targetEncoding;
			return targetEncoding.GetString(bytes, num2, bytes.Length - num2);
		}

		// Token: 0x0200025E RID: 606
		internal enum CreateOptions
		{
			// Token: 0x040008E0 RID: 2272
			None,
			// Token: 0x040008E1 RID: 2273
			CreateNativeObject
		}

		// Token: 0x0200025F RID: 607
		private static class EncodingUtility
		{
			// Token: 0x06001991 RID: 6545 RVA: 0x0002AF94 File Offset: 0x00029194
			static EncodingUtility()
			{
				Encoding encoding = new UTF32Encoding(true, true, true);
				Encoding encoding2 = new UTF32Encoding(false, true, true);
				Encoding encoding3 = new UnicodeEncoding(true, true, true);
				Encoding encoding4 = new UnicodeEncoding(false, true, true);
				Encoding encoding5 = new UTF8Encoding(true, true);
				TextAsset.EncodingUtility.encodingLookup = new KeyValuePair<byte[], Encoding>[]
				{
					new KeyValuePair<byte[], Encoding>(encoding.GetPreamble(), encoding),
					new KeyValuePair<byte[], Encoding>(encoding2.GetPreamble(), encoding2),
					new KeyValuePair<byte[], Encoding>(encoding3.GetPreamble(), encoding3),
					new KeyValuePair<byte[], Encoding>(encoding4.GetPreamble(), encoding4),
					new KeyValuePair<byte[], Encoding>(encoding5.GetPreamble(), encoding5)
				};
			}

			// Token: 0x040008E2 RID: 2274
			internal static readonly KeyValuePair<byte[], Encoding>[] encodingLookup;

			// Token: 0x040008E3 RID: 2275
			internal static readonly Encoding targetEncoding = Encoding.GetEncoding(Encoding.UTF8.CodePage, new EncoderReplacementFallback("\ufffd"), new DecoderReplacementFallback("\ufffd"));
		}
	}
}
