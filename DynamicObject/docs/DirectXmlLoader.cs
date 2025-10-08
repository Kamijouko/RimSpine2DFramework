using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using RimWorld.IO;
using UnityEngine;

namespace Verse
{
	// Token: 0x0200082D RID: 2093
	public static class DirectXmlLoader
	{
		// Token: 0x0600351C RID: 13596 RVA: 0x001170C4 File Offset: 0x001152C4
		public static LoadableXmlAsset[] XmlAssetsInModFolder(ModContentPack mod, string folderPath, List<string> foldersToLoadDebug = null)
		{
			List<string> list = foldersToLoadDebug ?? mod.foldersToLoadDescendingOrder;
			Dictionary<string, FileInfo> dictionary = new Dictionary<string, FileInfo>();
			for (int i = 0; i < list.Count; i++)
			{
				string text = list[i];
				DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(text, folderPath));
				if (directoryInfo.Exists)
				{
					foreach (FileInfo fileInfo in directoryInfo.GetFiles("*.xml", SearchOption.AllDirectories))
					{
						string text2 = fileInfo.FullName.Substring(text.Length + 1);
						dictionary.TryAdd(text2, fileInfo);
					}
				}
			}
			if (dictionary.Count == 0)
			{
				return DirectXmlLoader.EmptyXmlAssetsArray;
			}
			List<FileInfo> list2 = dictionary.Values.ToList<FileInfo>();
			LoadableXmlAsset[] assets = new LoadableXmlAsset[list2.Count];
			ConcurrentBag<KeyValuePair<int, FileInfo>> toLoad = new ConcurrentBag<KeyValuePair<int, FileInfo>>();
			for (int k = 0; k < list2.Count; k++)
			{
				int num = k;
				FileInfo fileInfo2 = list2[k];
				toLoad.Add(new KeyValuePair<int, FileInfo>(num, fileInfo2));
			}
			Thread[] array = new Thread[2];
			ThreadStart <>9__0;
			for (int l = 0; l < array.Length; l++)
			{
				Thread[] array2 = array;
				int num2 = l;
				ThreadStart threadStart;
				if ((threadStart = <>9__0) == null)
				{
					threadStart = (<>9__0 = delegate
					{
						KeyValuePair<int, FileInfo> keyValuePair2;
						while (toLoad.TryTake(out keyValuePair2))
						{
							int key2 = keyValuePair2.Key;
							FileInfo value2 = keyValuePair2.Value;
							assets[key2] = new LoadableXmlAsset(value2, mod);
						}
					});
				}
				array2[num2] = new Thread(threadStart)
				{
					Name = string.Format("DirectXmlLoader Thread {0} of {1}", l + 1, 2)
				};
				array[l].Start();
			}
			KeyValuePair<int, FileInfo> keyValuePair;
			while (toLoad.TryTake(out keyValuePair))
			{
				int key = keyValuePair.Key;
				FileInfo value = keyValuePair.Value;
				assets[key] = new LoadableXmlAsset(value, mod);
			}
			Thread[] array3 = array;
			for (int m = 0; m < array3.Length; m++)
			{
				array3[m].Join();
			}
			return assets;
		}

		// Token: 0x0600351D RID: 13597 RVA: 0x001172B3 File Offset: 0x001154B3
		public static IEnumerable<T> LoadXmlDataInResourcesFolder<T>(string folderPath) where T : new()
		{
			XmlInheritance.Clear();
			DeepProfiler.Start("Resources.LoadAll<TextAsset>");
			TextAsset[] array = Resources.LoadAll<TextAsset>(folderPath);
			DeepProfiler.End();
			DeepProfiler.Start("Load XML");
			List<LoadableXmlAsset> assets = (from x in array.Select((TextAsset x) => new { x.name, x.text }).ToList().AsParallel()
				select new LoadableXmlAsset(x.name, x.text)).ToList<LoadableXmlAsset>();
			DeepProfiler.End();
			DeepProfiler.Start("Resolve inheritance");
			foreach (LoadableXmlAsset loadableXmlAsset in assets)
			{
				XmlInheritance.TryRegisterAllFrom(loadableXmlAsset, null);
			}
			XmlInheritance.Resolve();
			DeepProfiler.End();
			DeepProfiler.Start("Read game items from XML");
			int num;
			for (int i = 0; i < assets.Count; i = num + 1)
			{
				foreach (T t in DirectXmlLoader.AllGameItemsFromAsset<T>(assets[i]))
				{
					yield return t;
				}
				IEnumerator<T> enumerator2 = null;
				num = i;
			}
			DeepProfiler.End();
			XmlInheritance.Clear();
			yield break;
			yield break;
		}

		// Token: 0x0600351E RID: 13598 RVA: 0x001172C3 File Offset: 0x001154C3
		public static T ItemFromXmlFile<T>(string filePath, bool resolveCrossRefs = true) where T : new()
		{
			if (!new FileInfo(filePath).Exists)
			{
				return new T();
			}
			return DirectXmlLoader.ItemFromXmlString<T>(File.ReadAllText(filePath), filePath, resolveCrossRefs);
		}

		// Token: 0x0600351F RID: 13599 RVA: 0x001172E5 File Offset: 0x001154E5
		public static T ItemFromXmlFile<T>(VirtualDirectory directory, string filePath, bool resolveCrossRefs = true) where T : new()
		{
			if (!directory.FileExists(filePath))
			{
				return new T();
			}
			return DirectXmlLoader.ItemFromXmlString<T>(directory.ReadAllText(filePath), directory.FullPath + "/" + filePath, resolveCrossRefs);
		}

		// Token: 0x06003520 RID: 13600 RVA: 0x00117314 File Offset: 0x00115514
		public static T ItemFromXmlString<T>(string xmlContent, string filePath, bool resolveCrossRefs = true) where T : new()
		{
			if (resolveCrossRefs && DirectXmlCrossRefLoader.LoadingInProgress)
			{
				Log.Error("Cannot call ItemFromXmlString with resolveCrossRefs=true while loading is already in progress (forgot to resolve or clear cross refs from previous loading?).");
			}
			T t2;
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(xmlContent);
				T t = DirectXmlToObject.ObjectFromXml<T>(xmlDocument.DocumentElement, false);
				if (resolveCrossRefs)
				{
					try
					{
						DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences(FailMode.LogErrors);
					}
					finally
					{
						DirectXmlCrossRefLoader.Clear();
					}
				}
				t2 = t;
			}
			catch (Exception ex)
			{
				Log.Error("Exception loading file at " + filePath + ". Loading defaults instead. Exception was: " + ex.ToString());
				t2 = new T();
			}
			return t2;
		}

		// Token: 0x06003521 RID: 13601 RVA: 0x001173A4 File Offset: 0x001155A4
		public static Def DefFromNode(XmlNode node, LoadableXmlAsset loadingAsset)
		{
			if (node.NodeType != XmlNodeType.Element)
			{
				return null;
			}
			XmlAttribute xmlAttribute = node.Attributes["Abstract"];
			if (xmlAttribute != null && xmlAttribute.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
			{
				return null;
			}
			XmlNode resolvedNodeFor = XmlInheritance.GetResolvedNodeFor(node);
			string text = node.Name;
			XmlAttribute xmlAttribute2 = resolvedNodeFor.Attributes["Class"];
			if (xmlAttribute2 != null)
			{
				text = xmlAttribute2.Value;
			}
			Type typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(node.Name, null);
			if (typeInAnyAssembly == null || !GenTypes.IsDef(typeInAnyAssembly))
			{
				Log.ErrorOnce(string.Concat(new string[]
				{
					"Type ",
					text,
					" is not a Def type or could not be found, in file ",
					(loadingAsset != null) ? loadingAsset.name : "(unknown)",
					". Context: ",
					node.OuterXml
				}), text.GetHashCode());
				return null;
			}
			Func<XmlNode, bool, object> objectFromXmlMethod = DirectXmlToObject.GetObjectFromXmlMethod(typeInAnyAssembly);
			Def def = null;
			try
			{
				def = (Def)objectFromXmlMethod(node, true);
				def.ResolveDefNameHash();
			}
			catch (Exception ex)
			{
				string text2 = "Exception loading def from file ";
				string text3 = ((loadingAsset != null) ? loadingAsset.name : "(unknown)");
				string text4 = ": ";
				Exception ex2 = ex;
				Log.Error(text2 + text3 + text4 + ((ex2 != null) ? ex2.ToString() : null));
			}
			return def;
		}

		// Token: 0x06003522 RID: 13602 RVA: 0x001174E8 File Offset: 0x001156E8
		public static IEnumerable<T> AllGameItemsFromAsset<T>(LoadableXmlAsset asset) where T : new()
		{
			if (asset.xmlDoc == null)
			{
				yield break;
			}
			XmlNodeList xmlNodeList = asset.xmlDoc.DocumentElement.SelectNodes(typeof(T).Name);
			bool gotData = false;
			foreach (object obj in xmlNodeList)
			{
				XmlNode xmlNode = (XmlNode)obj;
				XmlAttribute xmlAttribute = xmlNode.Attributes["Abstract"];
				if (xmlAttribute == null || !xmlAttribute.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
				{
					DeepProfiler.Start("DirectXmlToObject.ObjectFromXml<" + typeof(T).Name + ">");
					T t;
					try
					{
						t = DirectXmlToObject.ObjectFromXml<T>(xmlNode, true);
						gotData = true;
					}
					catch (Exception ex)
					{
						string text = "Exception loading data from file ";
						string name = asset.name;
						string text2 = ": ";
						Exception ex2 = ex;
						Log.Error(text + name + text2 + ((ex2 != null) ? ex2.ToString() : null));
						continue;
					}
					finally
					{
						DeepProfiler.End();
					}
					yield return t;
				}
			}
			IEnumerator enumerator = null;
			if (!gotData)
			{
				string text3 = "Found no usable data when trying to get ";
				Type typeFromHandle = typeof(T);
				Log.Error(text3 + ((typeFromHandle != null) ? typeFromHandle.ToString() : null) + "s from file " + asset.name);
			}
			yield break;
			yield break;
		}

		// Token: 0x04002825 RID: 10277
		private static readonly LoadableXmlAsset[] EmptyXmlAssetsArray = Array.Empty<LoadableXmlAsset>();
	}
}
