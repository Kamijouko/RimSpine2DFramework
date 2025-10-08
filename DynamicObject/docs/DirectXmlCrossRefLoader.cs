using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using UnityEngine;

namespace Verse
{
	// Token: 0x02000827 RID: 2087
	public static class DirectXmlCrossRefLoader
	{
		// Token: 0x06003500 RID: 13568 RVA: 0x001162D0 File Offset: 0x001144D0
		public static bool MistypedMayRequire(string mayRequireMod)
		{
			if (!Application.isEditor)
			{
				return false;
			}
			if (mayRequireMod.NullOrEmpty())
			{
				return false;
			}
			if (mayRequireMod.Contains(','))
			{
				string[] array = mayRequireMod.Split(',', StringSplitOptions.None);
				for (int i = 0; i < array.Length; i++)
				{
					if (!DirectXmlCrossRefLoader.<MistypedMayRequire>g__ExpansionFound|0_0(array[i]))
					{
						return true;
					}
				}
			}
			else if (!DirectXmlCrossRefLoader.<MistypedMayRequire>g__ExpansionFound|0_0(mayRequireMod))
			{
				return true;
			}
			return false;
		}

		// Token: 0x170009EF RID: 2543
		// (get) Token: 0x06003501 RID: 13569 RVA: 0x0011632B File Offset: 0x0011452B
		public static bool LoadingInProgress
		{
			get
			{
				return DirectXmlCrossRefLoader.wantedRefs.Count > 0;
			}
		}

		// Token: 0x06003502 RID: 13570 RVA: 0x0011633C File Offset: 0x0011453C
		public static void RegisterObjectWantsCrossRef(object wanter, FieldInfo fi, string targetDefName, string mayRequireMod = null, string mayRequireAnyMod = null, Type assumeFieldType = null)
		{
			DeepProfiler.Start("RegisterObjectWantsCrossRef (object, FieldInfo, string)");
			try
			{
				if (wanter.GetType().IsValueType)
				{
					Log.Error(string.Format("Cannot use value types for object cross reference. {0} is a value type but wants a cross reference to {1} {2} via field {3}.", new object[]
					{
						wanter.GetType(),
						fi.FieldType,
						targetDefName,
						fi.Name
					}));
				}
				DirectXmlCrossRefLoader.WantedRefForObject wantedRefForObject = new DirectXmlCrossRefLoader.WantedRefForObject(wanter, fi, targetDefName, mayRequireMod, mayRequireAnyMod, assumeFieldType);
				DirectXmlCrossRefLoader.wantedRefs.Add(wantedRefForObject);
			}
			finally
			{
				DeepProfiler.End();
			}
		}

		// Token: 0x06003503 RID: 13571 RVA: 0x001163C8 File Offset: 0x001145C8
		public static void RegisterObjectWantsCrossRef(object wanter, string fieldName, string targetDefName, string mayRequireMod = null, string mayRequireAnyMod = null, Type overrideFieldType = null)
		{
			DeepProfiler.Start("RegisterObjectWantsCrossRef (object,string,string)");
			try
			{
				FieldInfo field = wanter.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (wanter.GetType().IsValueType)
				{
					Log.Error(string.Format("Cannot use value types for object cross reference. {0} is a value type but wants a cross reference to {1} {2} via field {3}.", new object[]
					{
						wanter.GetType(),
						(field != null) ? field.FieldType : null,
						targetDefName,
						((field != null) ? field.Name : null) ?? fieldName
					}));
				}
				DirectXmlCrossRefLoader.WantedRefForObject wantedRefForObject = new DirectXmlCrossRefLoader.WantedRefForObject(wanter, field, targetDefName, mayRequireMod, mayRequireAnyMod, overrideFieldType);
				DirectXmlCrossRefLoader.wantedRefs.Add(wantedRefForObject);
			}
			finally
			{
				DeepProfiler.End();
			}
		}

		// Token: 0x06003504 RID: 13572 RVA: 0x00116474 File Offset: 0x00114674
		public static void RegisterObjectWantsCrossRef(object wanter, string fieldName, XmlNode parentNode, string mayRequireMod = null, string mayRequireAnyMod = null, Type overrideFieldType = null)
		{
			DeepProfiler.Start("RegisterObjectWantsCrossRef (object,string,XmlNode)");
			try
			{
				if (wanter.GetType().IsValueType)
				{
					Log.Error(string.Format("Cannot use value types for object cross reference. {0} is a value type but wants a cross reference to {1} via field {2}.", wanter.GetType(), parentNode.Name, fieldName));
				}
				string text = mayRequireMod;
				if (mayRequireMod == null)
				{
					XmlAttributeCollection attributes = parentNode.Attributes;
					if (attributes == null)
					{
						text = null;
					}
					else
					{
						XmlAttribute xmlAttribute = attributes["MayRequire"];
						text = ((xmlAttribute != null) ? xmlAttribute.Value.ToLower() : null);
					}
				}
				string text2 = text;
				string text3 = mayRequireAnyMod;
				if (mayRequireAnyMod == null)
				{
					XmlAttributeCollection attributes2 = parentNode.Attributes;
					if (attributes2 == null)
					{
						text3 = null;
					}
					else
					{
						XmlAttribute xmlAttribute2 = attributes2["MayRequireAnyOf"];
						text3 = ((xmlAttribute2 != null) ? xmlAttribute2.Value.ToLower() : null);
					}
				}
				string text4 = text3;
				DirectXmlCrossRefLoader.WantedRefForObject wantedRefForObject = new DirectXmlCrossRefLoader.WantedRefForObject(wanter, wanter.GetType().GetField(fieldName), parentNode.Name, text2, text4, overrideFieldType);
				DirectXmlCrossRefLoader.wantedRefs.Add(wantedRefForObject);
			}
			finally
			{
				DeepProfiler.End();
			}
		}

		// Token: 0x06003505 RID: 13573 RVA: 0x00116554 File Offset: 0x00114754
		public static void RegisterListWantsCrossRef<T>(List<T> wanterList, string targetDefName, object debugWanterInfo = null, string mayRequireMod = null, string mayRequireAnyMod = null)
		{
			DeepProfiler.Start("RegisterListWantsCrossRef");
			try
			{
				DirectXmlCrossRefLoader.WantedRef wantedRef;
				DirectXmlCrossRefLoader.WantedRefForList<T> wantedRefForList;
				if (!DirectXmlCrossRefLoader.wantedListDictRefs.TryGetValue(wanterList, out wantedRef))
				{
					wantedRefForList = new DirectXmlCrossRefLoader.WantedRefForList<T>(wanterList, debugWanterInfo);
					DirectXmlCrossRefLoader.wantedListDictRefs.Add(wanterList, wantedRefForList);
					DirectXmlCrossRefLoader.wantedRefs.Add(wantedRefForList);
				}
				else
				{
					wantedRefForList = (DirectXmlCrossRefLoader.WantedRefForList<T>)wantedRef;
				}
				wantedRefForList.AddWantedListEntry(targetDefName, mayRequireMod, mayRequireAnyMod);
			}
			finally
			{
				DeepProfiler.End();
			}
		}

		// Token: 0x06003506 RID: 13574 RVA: 0x001165C8 File Offset: 0x001147C8
		public static void RegisterDictionaryWantsCrossRef<K, V>(Dictionary<K, V> wanterDict, XmlNode entryNode, object debugWanterInfo = null)
		{
			DeepProfiler.Start("RegisterDictionaryWantsCrossRef");
			try
			{
				DirectXmlCrossRefLoader.WantedRef wantedRef;
				DirectXmlCrossRefLoader.WantedRefForDictionary<K, V> wantedRefForDictionary;
				if (!DirectXmlCrossRefLoader.wantedListDictRefs.TryGetValue(wanterDict, out wantedRef))
				{
					wantedRefForDictionary = new DirectXmlCrossRefLoader.WantedRefForDictionary<K, V>(wanterDict, debugWanterInfo);
					DirectXmlCrossRefLoader.wantedRefs.Add(wantedRefForDictionary);
					DirectXmlCrossRefLoader.wantedListDictRefs.Add(wanterDict, wantedRefForDictionary);
				}
				else
				{
					wantedRefForDictionary = (DirectXmlCrossRefLoader.WantedRefForDictionary<K, V>)wantedRef;
				}
				wantedRefForDictionary.AddWantedDictEntry(entryNode);
			}
			finally
			{
				DeepProfiler.End();
			}
		}

		// Token: 0x06003507 RID: 13575 RVA: 0x00116638 File Offset: 0x00114838
		public static T TryResolveDef<T>(string defName, FailMode failReportMode, object debugWanterInfo = null)
		{
			DeepProfiler.Start("TryResolveDef");
			T t2;
			try
			{
				T t = (T)((object)GenDefDatabase.GetDefSilentFail(typeof(T), defName, true));
				if (t != null)
				{
					t2 = t;
				}
				else
				{
					if (failReportMode == FailMode.LogErrors)
					{
						string text = "Could not resolve cross-reference to ";
						Type typeFromHandle = typeof(T);
						string text2 = text + ((typeFromHandle != null) ? typeFromHandle.ToString() : null) + " named " + defName.ToStringSafe<string>();
						if (debugWanterInfo != null)
						{
							text2 = text2 + " (wanter=" + debugWanterInfo.ToStringSafe<object>() + ")";
						}
						Log.Error(text2);
					}
					t2 = default(T);
				}
			}
			finally
			{
				DeepProfiler.End();
			}
			return t2;
		}

		// Token: 0x06003508 RID: 13576 RVA: 0x001166E8 File Offset: 0x001148E8
		public static void Clear()
		{
			DeepProfiler.Start("Clear");
			try
			{
				DirectXmlCrossRefLoader.wantedRefs.Clear();
				DirectXmlCrossRefLoader.wantedListDictRefs.Clear();
			}
			finally
			{
				DeepProfiler.End();
			}
		}

		// Token: 0x06003509 RID: 13577 RVA: 0x0011672C File Offset: 0x0011492C
		public static void ResolveAllWantedCrossReferences(FailMode failReportMode)
		{
			DeepProfiler.Start("ResolveAllWantedCrossReferences");
			try
			{
				HashSet<DirectXmlCrossRefLoader.WantedRef> resolvedRefs = new HashSet<DirectXmlCrossRefLoader.WantedRef>();
				object resolvedRefsLock = new object();
				DeepProfiler.enabled = false;
				GenThreading.ParallelForEach<DirectXmlCrossRefLoader.WantedRef>(DirectXmlCrossRefLoader.wantedRefs, delegate(DirectXmlCrossRefLoader.WantedRef wantedRef)
				{
					if (wantedRef.TryResolve(failReportMode))
					{
						object resolvedRefsLock2 = resolvedRefsLock;
						lock (resolvedRefsLock2)
						{
							resolvedRefs.Add(wantedRef);
						}
					}
				}, -1);
				foreach (DirectXmlCrossRefLoader.WantedRef wantedRef2 in resolvedRefs)
				{
					wantedRef2.Apply();
				}
				DirectXmlCrossRefLoader.wantedRefs.RemoveAll((DirectXmlCrossRefLoader.WantedRef x) => resolvedRefs.Contains(x));
				DeepProfiler.enabled = true;
			}
			finally
			{
				DeepProfiler.End();
			}
		}

		// Token: 0x0600350B RID: 13579 RVA: 0x00116814 File Offset: 0x00114A14
		[CompilerGenerated]
		internal static bool <MistypedMayRequire>g__ExpansionFound|0_0(string modID)
		{
			for (int i = 0; i < ModContentPack.ProductPackageIDs.Length; i++)
			{
				if (modID.EqualsIgnoreCase(ModContentPack.ProductPackageIDs[i]))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x04002812 RID: 10258
		private static List<DirectXmlCrossRefLoader.WantedRef> wantedRefs = new List<DirectXmlCrossRefLoader.WantedRef>();

		// Token: 0x04002813 RID: 10259
		private static Dictionary<object, DirectXmlCrossRefLoader.WantedRef> wantedListDictRefs = new Dictionary<object, DirectXmlCrossRefLoader.WantedRef>();

		// Token: 0x02000828 RID: 2088
		private abstract class WantedRef
		{
			// Token: 0x0600350C RID: 13580
			public abstract bool TryResolve(FailMode failReportMode);

			// Token: 0x0600350D RID: 13581 RVA: 0x000026EA File Offset: 0x000008EA
			public virtual void Apply()
			{
			}

			// Token: 0x04002814 RID: 10260
			public object wanter;
		}

		// Token: 0x02000829 RID: 2089
		private class WantedRefForObject : DirectXmlCrossRefLoader.WantedRef
		{
			// Token: 0x170009F0 RID: 2544
			// (get) Token: 0x0600350F RID: 13583 RVA: 0x00116848 File Offset: 0x00114A48
			private bool BadCrossRefAllowed
			{
				get
				{
					return (!this.mayRequireMod.NullOrEmpty() && !ModLister.AllModsActiveNoSuffix(this.mayRequireMod.Split(',', StringSplitOptions.None))) || (!this.mayRequireAnyMod.NullOrEmpty<string>() && !ModLister.AnyModActiveNoSuffix(this.mayRequireAnyMod));
				}
			}

			// Token: 0x06003510 RID: 13584 RVA: 0x00116898 File Offset: 0x00114A98
			public WantedRefForObject(object wanter, FieldInfo fi, string targetDefName, string mayRequireMod = null, string mayRequireAnyMod = null, Type overrideFieldType = null)
			{
				this.wanter = wanter;
				this.fi = fi;
				this.defName = targetDefName;
				this.mayRequireMod = mayRequireMod;
				this.overrideFieldType = overrideFieldType;
				this.mayRequireAnyMod = ((mayRequireAnyMod != null) ? mayRequireAnyMod.ToLower().Split(',', StringSplitOptions.None) : null);
			}

			// Token: 0x06003511 RID: 13585 RVA: 0x001168EC File Offset: 0x00114AEC
			public override bool TryResolve(FailMode failReportMode)
			{
				if (this.fi == null)
				{
					Log.Error("Trying to resolve null field for def named " + this.defName.ToStringSafe<string>());
					return false;
				}
				Type type = this.overrideFieldType ?? this.fi.FieldType;
				this.resolvedDef = GenDefDatabase.GetDefSilentFail(type, this.defName, true);
				if (DirectXmlCrossRefLoader.MistypedMayRequire(this.mayRequireMod))
				{
					Log.Error("Faulty MayRequire at def " + this.defName.ToStringSafe<string>() + ": " + this.mayRequireMod);
				}
				if (!this.mayRequireAnyMod.NullOrEmpty<string>())
				{
					foreach (string text in this.mayRequireAnyMod)
					{
						if (DirectXmlCrossRefLoader.MistypedMayRequire(text))
						{
							Log.Error("Faulty MayRequire at def " + this.defName.ToStringSafe<string>() + ": " + text);
						}
					}
				}
				if (this.resolvedDef == null)
				{
					if (failReportMode == FailMode.LogErrors && !this.BadCrossRefAllowed)
					{
						string[] array2 = new string[8];
						array2[0] = "Could not resolve cross-reference: No ";
						int num = 1;
						Type type2 = type;
						array2[num] = ((type2 != null) ? type2.ToString() : null);
						array2[2] = " named ";
						array2[3] = this.defName.ToStringSafe<string>();
						array2[4] = " found to give to ";
						int num2 = 5;
						Type type3 = this.wanter.GetType();
						array2[num2] = ((type3 != null) ? type3.ToString() : null);
						array2[6] = " ";
						array2[7] = this.wanter.ToStringSafe<object>();
						Log.Error(string.Concat(array2));
					}
					return false;
				}
				SoundDef soundDef = this.resolvedDef as SoundDef;
				if (soundDef != null && soundDef.isUndefined)
				{
					string[] array3 = new string[9];
					array3[0] = "Could not resolve cross-reference: No ";
					int num3 = 1;
					Type type4 = type;
					array3[num3] = ((type4 != null) ? type4.ToString() : null);
					array3[2] = " named ";
					array3[3] = this.defName.ToStringSafe<string>();
					array3[4] = " found to give to ";
					int num4 = 5;
					Type type5 = this.wanter.GetType();
					array3[num4] = ((type5 != null) ? type5.ToString() : null);
					array3[6] = " ";
					array3[7] = this.wanter.ToStringSafe<object>();
					array3[8] = " (using undefined sound instead)";
					Log.Warning(string.Concat(array3));
				}
				this.fi.SetValue(this.wanter, this.resolvedDef);
				return true;
			}

			// Token: 0x04002815 RID: 10261
			private readonly FieldInfo fi;

			// Token: 0x04002816 RID: 10262
			private readonly string defName;

			// Token: 0x04002817 RID: 10263
			private readonly string mayRequireMod;

			// Token: 0x04002818 RID: 10264
			private readonly string[] mayRequireAnyMod;

			// Token: 0x04002819 RID: 10265
			private readonly Type overrideFieldType;

			// Token: 0x0400281A RID: 10266
			private Def resolvedDef;
		}

		// Token: 0x0200082A RID: 2090
		private class WantedRefForList<T> : DirectXmlCrossRefLoader.WantedRef
		{
			// Token: 0x06003512 RID: 13586 RVA: 0x00116B0F File Offset: 0x00114D0F
			public WantedRefForList(object wanter, object debugWanterInfo)
			{
				this.wanter = wanter;
				this.debugWanterInfo = debugWanterInfo;
			}

			// Token: 0x06003513 RID: 13587 RVA: 0x00116B30 File Offset: 0x00114D30
			public void AddWantedListEntry(string newTargetDefName, string mayRequireMod = null, string mayRequireAnyMod = null)
			{
				if (!mayRequireMod.NullOrEmpty() && this.mayRequireMods == null)
				{
					this.mayRequireMods = new List<string>();
					for (int i = 0; i < this.defNames.Count; i++)
					{
						this.mayRequireMods.Add(null);
					}
				}
				if (!mayRequireAnyMod.NullOrEmpty() && this.mayRequireModsAny == null)
				{
					this.mayRequireModsAny = new Dictionary<string, List<string>>();
					for (int j = 0; j < this.defNames.Count; j++)
					{
						this.mayRequireModsAny.Add(this.defNames[j], new List<string>());
					}
				}
				this.defNames.Add(newTargetDefName);
				if (this.mayRequireMods != null)
				{
					this.mayRequireMods.Add(mayRequireMod);
				}
				if (this.mayRequireModsAny != null)
				{
					foreach (string text in mayRequireAnyMod.ToLower().Split(',', StringSplitOptions.None))
					{
						List<string> list;
						if (this.mayRequireModsAny.TryGetValue(newTargetDefName, out list))
						{
							list.Add(text.Trim());
						}
						else
						{
							this.mayRequireModsAny.Add(newTargetDefName, new List<string> { text.Trim() });
						}
					}
				}
			}

			// Token: 0x06003514 RID: 13588 RVA: 0x00116C50 File Offset: 0x00114E50
			public override bool TryResolve(FailMode failReportMode)
			{
				bool flag = false;
				for (int i = 0; i < this.defNames.Count; i++)
				{
					bool flag2 = this.mayRequireMods != null && i < this.mayRequireMods.Count && !this.mayRequireMods[i].NullOrEmpty() && !ModLister.AllModsActiveNoSuffix(this.mayRequireMods[i].Split(',', StringSplitOptions.None));
					List<string> list;
					if (this.mayRequireModsAny != null && this.mayRequireModsAny.TryGetValue(this.defNames[i], out list) && !ModLister.AnyModActiveNoSuffix(list))
					{
						flag2 = true;
					}
					if (this.mayRequireMods != null && i < this.mayRequireMods.Count && DirectXmlCrossRefLoader.MistypedMayRequire(this.mayRequireMods[i]))
					{
						Log.Error("Faulty MayRequire: " + this.mayRequireMods[i]);
					}
					T t = DirectXmlCrossRefLoader.TryResolveDef<T>(this.defNames[i], flag2 ? FailMode.Silent : failReportMode, this.debugWanterInfo);
					if (t != null)
					{
						((List<T>)this.wanter).Add(t);
						this.defNames.RemoveAt(i);
						if (this.mayRequireMods != null && i < this.mayRequireMods.Count)
						{
							this.mayRequireMods.RemoveAt(i);
						}
						i--;
					}
					else
					{
						flag = true;
					}
				}
				return !flag;
			}

			// Token: 0x0400281B RID: 10267
			private List<string> defNames = new List<string>();

			// Token: 0x0400281C RID: 10268
			private List<string> mayRequireMods;

			// Token: 0x0400281D RID: 10269
			private Dictionary<string, List<string>> mayRequireModsAny;

			// Token: 0x0400281E RID: 10270
			private object debugWanterInfo;
		}

		// Token: 0x0200082B RID: 2091
		private class WantedRefForDictionary<K, V> : DirectXmlCrossRefLoader.WantedRef
		{
			// Token: 0x06003515 RID: 13589 RVA: 0x00116DA8 File Offset: 0x00114FA8
			public WantedRefForDictionary(object wanter, object debugWanterInfo)
			{
				this.wanter = wanter;
				this.debugWanterInfo = debugWanterInfo;
			}

			// Token: 0x06003516 RID: 13590 RVA: 0x00116DD4 File Offset: 0x00114FD4
			public void AddWantedDictEntry(XmlNode entryNode)
			{
				this.wantedDictRefs.Add(entryNode);
			}

			// Token: 0x06003517 RID: 13591 RVA: 0x00116DE4 File Offset: 0x00114FE4
			public override bool TryResolve(FailMode failReportMode)
			{
				failReportMode = FailMode.LogErrors;
				bool flag = GenTypes.IsDef(typeof(K));
				bool flag2 = GenTypes.IsDef(typeof(V));
				foreach (XmlNode xmlNode in this.wantedDictRefs)
				{
					XmlNode xmlNode2 = xmlNode["key"];
					XmlNode xmlNode3 = xmlNode["value"];
					string text = ((xmlNode2 != null) ? xmlNode2.InnerText : null);
					string text2 = ((xmlNode3 != null) ? xmlNode3.InnerText : null);
					object obj;
					object obj2;
					if (text == null || text2 == null)
					{
						if (failReportMode == FailMode.LogErrors)
						{
							string text3 = "Missing 'key' and/or 'value'.";
							if (this.debugWanterInfo != null)
							{
								text3 = text3 + " (wanter=" + this.debugWanterInfo.ToStringSafe<object>() + ")";
							}
							Log.Error(text3);
						}
						obj = default(K);
						obj2 = default(V);
					}
					else
					{
						if (flag)
						{
							obj = DirectXmlCrossRefLoader.TryResolveDef<K>(text, failReportMode, this.debugWanterInfo);
						}
						else
						{
							obj = xmlNode2;
						}
						if (flag2)
						{
							obj2 = DirectXmlCrossRefLoader.TryResolveDef<V>(text2, failReportMode, this.debugWanterInfo);
						}
						else
						{
							obj2 = xmlNode3;
						}
					}
					this.makingData.Add(new Pair<object, object>(obj, obj2));
				}
				return true;
			}

			// Token: 0x06003518 RID: 13592 RVA: 0x00116F50 File Offset: 0x00115150
			public override void Apply()
			{
				Dictionary<K, V> dictionary = (Dictionary<K, V>)this.wanter;
				dictionary.Clear();
				foreach (Pair<object, object> pair in this.makingData)
				{
					try
					{
						object obj = pair.First;
						object obj2 = pair.Second;
						if (obj is XmlNode)
						{
							obj = DirectXmlToObject.ObjectFromXml<K>(obj as XmlNode, true);
						}
						if (obj2 is XmlNode)
						{
							obj2 = DirectXmlToObject.ObjectFromXml<V>(obj2 as XmlNode, true);
						}
						dictionary.Add((K)((object)obj), (V)((object)obj2));
					}
					catch
					{
						string text = "Failed to load key/value pair: ";
						object first = pair.First;
						string text2 = ((first != null) ? first.ToString() : null);
						string text3 = ", ";
						object second = pair.Second;
						Log.Error(text + text2 + text3 + ((second != null) ? second.ToString() : null));
					}
				}
			}

			// Token: 0x0400281F RID: 10271
			private List<XmlNode> wantedDictRefs = new List<XmlNode>();

			// Token: 0x04002820 RID: 10272
			private object debugWanterInfo;

			// Token: 0x04002821 RID: 10273
			private List<Pair<object, object>> makingData = new List<Pair<object, object>>();
		}
	}
}
