using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Verse
{
	// Token: 0x0200083B RID: 2107
	public static class DirectXmlToObjectNew
	{
		// Token: 0x06003563 RID: 13667 RVA: 0x001192F4 File Offset: 0x001174F4
		public static Def DefFromNodeNew(XmlNode node, LoadableXmlAsset loadingAsset)
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
			Type typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(text, null);
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
			Def def = null;
			try
			{
				DirectXmlToObjectNew.ParseValueAndReturnDefDelegate defParserForType = DirectXmlToObjectNew.GetDefParserForType(typeInAnyAssembly);
				DeepProfiler.Start(string.Format("ParseValueAndReturnDef (for {0})", typeInAnyAssembly));
				def = defParserForType(0, 0, node, typeInAnyAssembly);
				DeepProfiler.End();
				def.ResolveDefNameHash();
			}
			catch (Exception ex)
			{
				Log.Error(string.Format("Exception loading def from file {0}: {1}", (loadingAsset != null) ? loadingAsset.name : "(unknown)", ex));
			}
			return def;
		}

		// Token: 0x06003564 RID: 13668 RVA: 0x00119430 File Offset: 0x00117630
		public static DirectXmlToObjectNew.ParseValueAndSetFieldDelegate GetFieldSetterForType(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type", "Cannot get field setter for null type.");
			}
			if (DirectXmlToObjectNew.TypeCanBeDeserializedWithSharedBody(type))
			{
				type = typeof(object);
			}
			DirectXmlToObjectNew.ParseValueAndSetFieldDelegate parseValueAndSetFieldDelegate;
			if (DirectXmlToObjectNew.parseMethods.TryGetValue(type, out parseValueAndSetFieldDelegate))
			{
				return parseValueAndSetFieldDelegate;
			}
			DeepProfiler.Start("CreateFieldSetterForType");
			parseValueAndSetFieldDelegate = DirectXmlToObjectNew.CreateFieldSetterForType(type);
			DeepProfiler.End();
			DirectXmlToObjectNew.parseMethods[type] = parseValueAndSetFieldDelegate;
			return parseValueAndSetFieldDelegate;
		}

		// Token: 0x06003565 RID: 13669 RVA: 0x001194A0 File Offset: 0x001176A0
		public static DirectXmlToObjectNew.ParseValueAndAddListItemDelegate GetListItemAdderForType(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type", "Cannot get list item adder for null type.");
			}
			if (DirectXmlToObjectNew.TypeCanBeDeserializedWithSharedBody(type))
			{
				type = typeof(object);
			}
			DirectXmlToObjectNew.ParseValueAndAddListItemDelegate parseValueAndAddListItemDelegate;
			if (DirectXmlToObjectNew.parseListItemMethods.TryGetValue(type, out parseValueAndAddListItemDelegate))
			{
				return parseValueAndAddListItemDelegate;
			}
			DeepProfiler.Start("CreateListItemAdderForType");
			parseValueAndAddListItemDelegate = DirectXmlToObjectNew.CreateListItemAdderForType(type);
			DeepProfiler.End();
			DirectXmlToObjectNew.parseListItemMethods[type] = parseValueAndAddListItemDelegate;
			return parseValueAndAddListItemDelegate;
		}

		// Token: 0x06003566 RID: 13670 RVA: 0x00119510 File Offset: 0x00117710
		public static DirectXmlToObjectNew.ParseValueAndReturnDefDelegate GetDefParserForType(Type type)
		{
			if (DirectXmlToObjectNew.TypeCanBeDeserializedWithSharedBody(type))
			{
				type = typeof(Def);
			}
			DirectXmlToObjectNew.ParseValueAndReturnDefDelegate parseValueAndReturnDefDelegate;
			if (DirectXmlToObjectNew.parseDefMethods.TryGetValue(type, out parseValueAndReturnDefDelegate))
			{
				return parseValueAndReturnDefDelegate;
			}
			parseValueAndReturnDefDelegate = DirectXmlToObjectNew.CreateDefParserForType(type);
			DirectXmlToObjectNew.parseDefMethods[type] = parseValueAndReturnDefDelegate;
			return parseValueAndReturnDefDelegate;
		}

		// Token: 0x06003567 RID: 13671 RVA: 0x00119558 File Offset: 0x00117758
		private static DirectXmlToObjectNew.ParseValueAndSetFieldDelegate CreateFieldSetterForType(Type type)
		{
			if (XmlToObjectUtils.CustomDataLoadMethodOf(type) != null)
			{
				return DirectXmlToObjectNew.CreateFieldSetterForCustomLoadable(type);
			}
			if (GenTypes.IsSlateRef(type))
			{
				return DirectXmlToObjectNew.CreateFieldSetterForSlateRef(type);
			}
			if (type == typeof(string))
			{
				return DirectXmlToObjectNew.CreateFieldSetterForString();
			}
			if (GenTypes.HasFlagsAttribute(type))
			{
				return DirectXmlToObjectNew.CreateFieldSetterForFlagsEnum(type);
			}
			if (GenTypes.IsList(type))
			{
				return DirectXmlToObjectNew.CreateFieldSetterForList(type);
			}
			if (GenTypes.IsDictionary(type))
			{
				return DirectXmlToObjectNew.CreateFieldSetterForDict(type);
			}
			return DirectXmlToObjectNew.CreateFieldSetterForGeneralType(type);
		}

		// Token: 0x06003568 RID: 13672 RVA: 0x001195D4 File Offset: 0x001177D4
		private static DirectXmlToObjectNew.ParseValueAndAddListItemDelegate CreateListItemAdderForType(Type type)
		{
			if (XmlToObjectUtils.CustomDataLoadMethodOf(type) != null)
			{
				return DirectXmlToObjectNew.CreateListItemAdderForCustomLoadable(type);
			}
			if (GenTypes.IsSlateRef(type))
			{
				return DirectXmlToObjectNew.CreateListItemAdderForSlateRef(type);
			}
			if (type == typeof(string))
			{
				return DirectXmlToObjectNew.CreateListItemAdderForString();
			}
			if (GenTypes.HasFlagsAttribute(type))
			{
				return DirectXmlToObjectNew.CreateListItemAdderForFlagsEnum(type);
			}
			if (GenTypes.IsList(type))
			{
				return DirectXmlToObjectNew.CreateListItemAdderForList(type);
			}
			if (GenTypes.IsDictionary(type))
			{
				return DirectXmlToObjectNew.CreateListItemAdderForDict(type);
			}
			return DirectXmlToObjectNew.CreateListItemAdderForGeneralType(type);
		}

		// Token: 0x06003569 RID: 13673 RVA: 0x00119650 File Offset: 0x00117850
		private static DirectXmlToObjectNew.ParseValueAndReturnDefDelegate CreateDefParserForType(Type type)
		{
			if (!GenTypes.IsDef(type))
			{
				throw new InvalidOperationException("Cannot create a Def parser for type " + type.FullName + ". Only Def types are supported.");
			}
			string text = ((type == typeof(object)) ? "SharedBody" : DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type));
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndReturnDef_" + text, type, new Type[]
			{
				typeof(int),
				typeof(int),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			LocalBuilder localBuilder = ilgenerator.DeclareLocal(type);
			DirectXmlToObjectNew.EmitIlToCreateAndPopulateComplexType(ilgenerator, type, localBuilder);
			ilgenerator.Emit(OpCodes.Ldloc, localBuilder);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndReturnDefDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndReturnDefDelegate));
		}

		// Token: 0x0600356A RID: 13674 RVA: 0x00119738 File Offset: 0x00117938
		private static DirectXmlToObjectNew.ParseValueAndSetFieldDelegate CreateFieldSetterForCustomLoadable(Type type)
		{
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndSetCustomLoadableField_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type), null, new Type[]
			{
				typeof(object),
				typeof(FieldInfo),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_1);
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitIlToCreateCustomLoadable(ilgenerator, type);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.FieldSetValueMethod);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndSetFieldDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndSetFieldDelegate));
		}

		// Token: 0x0600356B RID: 13675 RVA: 0x001197F0 File Offset: 0x001179F0
		private static DirectXmlToObjectNew.ParseValueAndSetFieldDelegate CreateFieldSetterForSlateRef(Type slateRefType)
		{
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndSetSlateRefField_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(slateRefType), null, new Type[]
			{
				typeof(object),
				typeof(FieldInfo),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_1);
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitIlToCreateSlateRef(ilgenerator, slateRefType);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.FieldSetValueMethod);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndSetFieldDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndSetFieldDelegate));
		}

		// Token: 0x0600356C RID: 13676 RVA: 0x001198A8 File Offset: 0x00117AA8
		private static DirectXmlToObjectNew.ParseValueAndSetFieldDelegate CreateFieldSetterForString()
		{
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndSetStringField", null, new Type[]
			{
				typeof(object),
				typeof(FieldInfo),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_1);
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitIlToCreateString(ilgenerator);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.FieldSetValueMethod);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndSetFieldDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndSetFieldDelegate));
		}

		// Token: 0x0600356D RID: 13677 RVA: 0x00119954 File Offset: 0x00117B54
		private static DirectXmlToObjectNew.ParseValueAndSetFieldDelegate CreateFieldSetterForFlagsEnum(Type type)
		{
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndSetFlagsEnumField_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type), null, new Type[]
			{
				typeof(object),
				typeof(FieldInfo),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			LocalBuilder localBuilder = ilgenerator.DeclareLocal(type);
			Label label = ilgenerator.DefineLabel();
			Label label2 = ilgenerator.DefineLabel();
			DirectXmlToObjectNew.EmitIlToHandleSingleTextNodeViaParseHelper(ilgenerator, type, localBuilder, label2, label);
			ilgenerator.MarkLabel(label2);
			DirectXmlToObjectNew.EmitIlToParseFlagsEnum(ilgenerator, type);
			ilgenerator.Emit(OpCodes.Unbox_Any, type);
			ilgenerator.Emit(OpCodes.Stloc, localBuilder);
			ilgenerator.MarkLabel(label);
			ilgenerator.Emit(OpCodes.Ldarg_1);
			ilgenerator.Emit(OpCodes.Ldarg_0);
			ilgenerator.Emit(OpCodes.Ldloc, localBuilder);
			ilgenerator.Emit(OpCodes.Box, type);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.FieldSetValueMethod);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndSetFieldDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndSetFieldDelegate));
		}

		// Token: 0x0600356E RID: 13678 RVA: 0x00119A6C File Offset: 0x00117C6C
		private static DirectXmlToObjectNew.ParseValueAndSetFieldDelegate CreateFieldSetterForList(Type listType)
		{
			Type type = listType.GetGenericArguments()[0];
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndSetListField_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type), null, new Type[]
			{
				typeof(object),
				typeof(FieldInfo),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_1);
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitIlToCreateAndPopulateList(ilgenerator, listType, type);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.FieldSetValueMethod);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndSetFieldDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndSetFieldDelegate));
		}

		// Token: 0x0600356F RID: 13679 RVA: 0x00119B30 File Offset: 0x00117D30
		private static DirectXmlToObjectNew.ParseValueAndSetFieldDelegate CreateFieldSetterForDict(Type dictType)
		{
			Type type = dictType.GetGenericArguments()[0];
			Type type2 = dictType.GetGenericArguments()[1];
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndSetDictField_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type) + "_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type2), null, new Type[]
			{
				typeof(object),
				typeof(FieldInfo),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_1);
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitILToCreateAndPopulateDictionary(ilgenerator, dictType, type, type2);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.FieldSetValueMethod);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndSetFieldDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndSetFieldDelegate));
		}

		// Token: 0x06003570 RID: 13680 RVA: 0x00119C08 File Offset: 0x00117E08
		private static DirectXmlToObjectNew.ParseValueAndSetFieldDelegate CreateFieldSetterForGeneralType(Type type)
		{
			string text = ((type == typeof(object)) ? "SharedBody" : DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type));
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndSetComplexTypeField_" + text, null, new Type[]
			{
				typeof(object),
				typeof(FieldInfo),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			Type innerIfNullable = type.GetInnerIfNullable();
			bool flag = innerIfNullable != type;
			LocalBuilder localBuilder = ilgenerator.DeclareLocal(type);
			LocalBuilder localBuilder2 = (flag ? ilgenerator.DeclareLocal(innerIfNullable) : null);
			Label label = ilgenerator.DefineLabel();
			Label label2 = ilgenerator.DefineLabel();
			Label label3 = ilgenerator.DefineLabel();
			Label label4 = ilgenerator.DefineLabel();
			Label label5 = ilgenerator.DefineLabel();
			DirectXmlToObjectNew.EmitIlToHandleEmptyNode(ilgenerator, type, localBuilder, label3, label2);
			ilgenerator.MarkLabel(label3);
			DirectXmlToObjectNew.EmitIlToErrorAndMakeDefaultValueForCdata(ilgenerator, type, label4, label, localBuilder);
			ilgenerator.MarkLabel(label4);
			if (ParseHelper.HandlesType(type))
			{
				DirectXmlToObjectNew.EmitIlToHandleSingleTextNodeViaParseHelper(ilgenerator, innerIfNullable, flag ? localBuilder2 : localBuilder, label5, label);
			}
			ilgenerator.MarkLabel(label5);
			bool flag2 = !innerIfNullable.IsPrimitive && !innerIfNullable.IsEnum && innerIfNullable != typeof(Type);
			bool flag3 = innerIfNullable.IsValueType || innerIfNullable.GetConstructor(Type.EmptyTypes) != null;
			if (flag2 && flag3)
			{
				DirectXmlToObjectNew.EmitIlToCreateAndPopulateComplexType(ilgenerator, innerIfNullable, flag ? localBuilder2 : localBuilder);
			}
			ilgenerator.MarkLabel(label);
			if (flag)
			{
				ilgenerator.Emit(OpCodes.Ldloc, localBuilder2);
				ilgenerator.Emit(OpCodes.Newobj, type.GetConstructor(new Type[] { innerIfNullable }));
				ilgenerator.Emit(OpCodes.Stloc, localBuilder);
			}
			ilgenerator.MarkLabel(label2);
			ilgenerator.Emit(OpCodes.Ldarg_1);
			ilgenerator.Emit(OpCodes.Ldarg_0);
			ilgenerator.Emit(OpCodes.Ldloc, localBuilder);
			if (type.IsValueType)
			{
				ilgenerator.Emit(OpCodes.Box, type);
			}
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.FieldSetValueMethod);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndSetFieldDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndSetFieldDelegate));
		}

		// Token: 0x06003571 RID: 13681 RVA: 0x00119E3C File Offset: 0x0011803C
		private static DirectXmlToObjectNew.ParseValueAndAddListItemDelegate CreateListItemAdderForCustomLoadable(Type type)
		{
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndAddCustomLoadableToList_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type), null, new Type[]
			{
				typeof(object),
				typeof(int),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitIlToCreateCustomLoadable(ilgenerator, type);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.IListAddItemMethod);
			ilgenerator.Emit(OpCodes.Pop);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndAddListItemDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndAddListItemDelegate));
		}

		// Token: 0x06003572 RID: 13682 RVA: 0x00119EF4 File Offset: 0x001180F4
		private static DirectXmlToObjectNew.ParseValueAndAddListItemDelegate CreateListItemAdderForSlateRef(Type slateRefType)
		{
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndAddSlateRefToList_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(slateRefType), null, new Type[]
			{
				typeof(object),
				typeof(int),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitIlToCreateSlateRef(ilgenerator, slateRefType);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.IListAddItemMethod);
			ilgenerator.Emit(OpCodes.Pop);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndAddListItemDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndAddListItemDelegate));
		}

		// Token: 0x06003573 RID: 13683 RVA: 0x00119FAC File Offset: 0x001181AC
		private static DirectXmlToObjectNew.ParseValueAndAddListItemDelegate CreateListItemAdderForString()
		{
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndAddStringToList", null, new Type[]
			{
				typeof(object),
				typeof(int),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitIlToCreateString(ilgenerator);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.IListAddItemMethod);
			ilgenerator.Emit(OpCodes.Pop);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndAddListItemDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndAddListItemDelegate));
		}

		// Token: 0x06003574 RID: 13684 RVA: 0x0011A058 File Offset: 0x00118258
		private static DirectXmlToObjectNew.ParseValueAndAddListItemDelegate CreateListItemAdderForFlagsEnum(Type type)
		{
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndAddFlagsEnumToList_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type), null, new Type[]
			{
				typeof(object),
				typeof(int),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitIlToParseFlagsEnum(ilgenerator, type);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.IListAddItemMethod);
			ilgenerator.Emit(OpCodes.Pop);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndAddListItemDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndAddListItemDelegate));
		}

		// Token: 0x06003575 RID: 13685 RVA: 0x0011A110 File Offset: 0x00118310
		private static DirectXmlToObjectNew.ParseValueAndAddListItemDelegate CreateListItemAdderForList(Type listType)
		{
			Type type = listType.GetGenericArguments()[0];
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndAddListToList_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type), null, new Type[]
			{
				typeof(object),
				typeof(int),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitIlToCreateAndPopulateList(ilgenerator, listType, type);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.IListAddItemMethod);
			ilgenerator.Emit(OpCodes.Pop);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndAddListItemDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndAddListItemDelegate));
		}

		// Token: 0x06003576 RID: 13686 RVA: 0x0011A1D4 File Offset: 0x001183D4
		private static DirectXmlToObjectNew.ParseValueAndAddListItemDelegate CreateListItemAdderForDict(Type dictType)
		{
			Type type = dictType.GetGenericArguments()[0];
			Type type2 = dictType.GetGenericArguments()[1];
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndAddDictToList_" + DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(dictType), null, new Type[]
			{
				typeof(object),
				typeof(int),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.Emit(OpCodes.Ldarg_0);
			DirectXmlToObjectNew.EmitILToCreateAndPopulateDictionary(ilgenerator, dictType, type, type2);
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.IListAddItemMethod);
			ilgenerator.Emit(OpCodes.Pop);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndAddListItemDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndAddListItemDelegate));
		}

		// Token: 0x06003577 RID: 13687 RVA: 0x0011A2A0 File Offset: 0x001184A0
		private static DirectXmlToObjectNew.ParseValueAndAddListItemDelegate CreateListItemAdderForGeneralType(Type type)
		{
			string text = ((type == typeof(object)) ? "SharedBody" : DirectXmlToObjectNew.GetDynamicMethodNameSuffixForType(type));
			DynamicMethod dynamicMethod = new DynamicMethod("ParseAndAddComplexTypeToList_" + text, null, new Type[]
			{
				typeof(object),
				typeof(int),
				typeof(XmlNode),
				typeof(Type)
			}, typeof(DirectXmlToObjectNew.DummyTypeToHoldDynamicMethods));
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			LocalBuilder localBuilder = ilgenerator.DeclareLocal(type);
			Label label = ilgenerator.DefineLabel();
			Label label2 = ilgenerator.DefineLabel();
			Label label3 = ilgenerator.DefineLabel();
			Label label4 = ilgenerator.DefineLabel();
			DirectXmlToObjectNew.EmitIlToHandleEmptyNode(ilgenerator, type, localBuilder, label2, label);
			ilgenerator.MarkLabel(label2);
			DirectXmlToObjectNew.EmitIlToErrorAndMakeDefaultValueForCdata(ilgenerator, type, label3, label, localBuilder);
			ilgenerator.MarkLabel(label3);
			if (ParseHelper.HandlesType(type))
			{
				DirectXmlToObjectNew.EmitIlToHandleSingleTextNodeViaParseHelper(ilgenerator, type, localBuilder, label4, label);
			}
			ilgenerator.MarkLabel(label4);
			DirectXmlToObjectNew.EmitIlToCreateAndPopulateComplexType(ilgenerator, type, localBuilder);
			ilgenerator.MarkLabel(label);
			ilgenerator.Emit(OpCodes.Ldarg_0);
			ilgenerator.Emit(OpCodes.Ldloc, localBuilder);
			if (type.IsValueType)
			{
				ilgenerator.Emit(OpCodes.Box, type);
			}
			ilgenerator.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.IListAddItemMethod);
			ilgenerator.Emit(OpCodes.Pop);
			ilgenerator.Emit(OpCodes.Ret);
			return (DirectXmlToObjectNew.ParseValueAndAddListItemDelegate)dynamicMethod.CreateDelegate(typeof(DirectXmlToObjectNew.ParseValueAndAddListItemDelegate));
		}

		// Token: 0x06003578 RID: 13688 RVA: 0x0011A408 File Offset: 0x00118608
		private static void EmitIlToCreateCustomLoadable(ILGenerator il, Type typeToInstantiate)
		{
			ConstructorInfo constructor = typeToInstantiate.GetConstructor(Type.EmptyTypes);
			MethodInfo methodInfo = XmlToObjectUtils.CustomDataLoadMethodOf(typeToInstantiate);
			if (constructor == null)
			{
				throw new InvalidOperationException("Type " + typeToInstantiate.FullName + " does not have a parameterless constructor, but needs one to use LoadDataFromXmlCustom.");
			}
			if (methodInfo == null)
			{
				throw new InvalidOperationException("Type " + typeToInstantiate.FullName + " does not have a method named LoadDataFromXmlCustom, but we are trying to create a parser for it using that method.");
			}
			il.Emit(OpCodes.Newobj, constructor);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.XmlInheritanceGetResolvedNodeForMethod);
			il.Emit(OpCodes.Call, methodInfo);
		}

		// Token: 0x06003579 RID: 13689 RVA: 0x0011A4B0 File Offset: 0x001186B0
		private static void EmitIlToCreateSlateRef(ILGenerator il, Type slateRefType)
		{
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.InnerTextWithReplacedNewlinesOrXmlMethod);
			il.Emit(OpCodes.Ldtoken, slateRefType);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.ParseHelperFromStringNonGenericMethod);
		}

		// Token: 0x0600357A RID: 13690 RVA: 0x0011A504 File Offset: 0x00118704
		private static void EmitIlToCreateString(ILGenerator il)
		{
			LocalBuilder localBuilder = il.DeclareLocal(typeof(XmlNodeType));
			LocalBuilder localBuilder2 = il.DeclareLocal(typeof(string));
			Label label = il.DefineLabel();
			Label label2 = il.DefineLabel();
			Label label3 = il.DefineLabel();
			Label label4 = il.DefineLabel();
			Label label5 = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeHasChildNodesMethod);
			il.Emit(OpCodes.Brtrue, label);
			il.Emit(OpCodes.Ldstr, "");
			il.Emit(OpCodes.Stloc, localBuilder2);
			il.Emit(OpCodes.Br, label5);
			il.MarkLabel(label);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetChildNodesMethod);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetCountMethod);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Beq, label2);
			il.Emit(OpCodes.Ldstr, "XML node has more than one child node, which is unsupported for string parsing. Context: ");
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetOuterXmlMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.StringConcatMethod);
			il.Emit(OpCodes.Newobj, DirectXmlToObjectNew.InvalidOperationExceptionStringConstructor);
			il.Emit(OpCodes.Throw);
			il.MarkLabel(label2);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetFirstChildMethod);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetNodeTypeMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder);
			il.Emit(OpCodes.Ldc_I4, 3);
			il.Emit(OpCodes.Beq, label3);
			il.Emit(OpCodes.Ldloc, localBuilder);
			il.Emit(OpCodes.Ldc_I4, 4);
			il.Emit(OpCodes.Beq, label4);
			il.Emit(OpCodes.Ldstr, "XML node has an unsupported child node type to parse as string. Expected Text or CDATA. Context: ");
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetOuterXmlMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.StringConcatMethod);
			il.Emit(OpCodes.Newobj, DirectXmlToObjectNew.InvalidOperationExceptionStringConstructor);
			il.Emit(OpCodes.Throw);
			il.MarkLabel(label3);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetInnerTextMethod);
			il.Emit(OpCodes.Ldtoken, typeof(string));
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.ParseHelperFromStringNonGenericMethod);
			il.Emit(OpCodes.Castclass, typeof(string));
			il.Emit(OpCodes.Stloc, localBuilder2);
			il.Emit(OpCodes.Br, label5);
			il.MarkLabel(label4);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetFirstChildMethod);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetValueMethod);
			il.Emit(OpCodes.Stloc, localBuilder2);
			il.Emit(OpCodes.Br, label5);
			il.MarkLabel(label5);
			il.Emit(OpCodes.Ldloc, localBuilder2);
		}

		// Token: 0x0600357B RID: 13691 RVA: 0x0011A80C File Offset: 0x00118A0C
		private static void EmitIlToParseFlagsEnum(ILGenerator il, Type enumType)
		{
			LocalBuilder localBuilder = il.DeclareLocal(typeof(XmlNode));
			LocalBuilder localBuilder2 = il.DeclareLocal(enumType);
			LocalBuilder localBuilder3 = il.DeclareLocal(typeof(XmlNodeList));
			LocalBuilder localBuilder4 = il.DeclareLocal(typeof(int));
			LocalBuilder localBuilder5 = il.DeclareLocal(typeof(int));
			LocalBuilder localBuilder6 = il.DeclareLocal(enumType);
			Label label = il.DefineLabel();
			Label label2 = il.DefineLabel();
			Label label3 = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetChildNodesMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder3);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetCountMethod);
			il.Emit(OpCodes.Stloc, localBuilder4);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stloc, localBuilder2);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stloc, localBuilder6);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stloc, localBuilder5);
			il.MarkLabel(label);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Bge, label3);
			il.Emit(OpCodes.Ldloc, localBuilder3);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetItemMethod);
			il.Emit(OpCodes.Stloc, localBuilder);
			il.Emit(OpCodes.Ldloc, localBuilder);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldtoken, enumType);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.ValidateListNodeMethod);
			il.Emit(OpCodes.Brfalse, label2);
			il.Emit(OpCodes.Ldloc, localBuilder);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetInnerTextMethod);
			il.Emit(OpCodes.Ldtoken, enumType);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.ParseHelperFromStringNonGenericMethod);
			il.Emit(OpCodes.Unbox_Any, enumType);
			il.Emit(OpCodes.Stloc, localBuilder6);
			il.Emit(OpCodes.Ldloc, localBuilder2);
			il.Emit(OpCodes.Ldloc, localBuilder6);
			il.Emit(OpCodes.Or);
			il.Emit(OpCodes.Stloc, localBuilder2);
			il.MarkLabel(label2);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Stloc, localBuilder5);
			il.Emit(OpCodes.Br, label);
			il.MarkLabel(label3);
			il.Emit(OpCodes.Ldloc, localBuilder2);
			il.Emit(OpCodes.Box, enumType);
		}

		// Token: 0x0600357C RID: 13692 RVA: 0x0011AAC8 File Offset: 0x00118CC8
		private static void EmitIlToCreateAndPopulateList(ILGenerator il, Type listType, Type itemType)
		{
			ConstructorInfo constructor = listType.GetConstructor(new Type[] { typeof(int) });
			bool flag = GenTypes.IsDef(itemType);
			LocalBuilder localBuilder = il.DeclareLocal(listType);
			LocalBuilder localBuilder2 = il.DeclareLocal(typeof(XmlNodeList));
			LocalBuilder localBuilder3 = il.DeclareLocal(typeof(int));
			LocalBuilder localBuilder4 = il.DeclareLocal(typeof(int));
			LocalBuilder localBuilder5 = il.DeclareLocal(typeof(XmlNode));
			LocalBuilder localBuilder6 = il.DeclareLocal(typeof(XmlAttribute));
			LocalBuilder localBuilder7 = il.DeclareLocal(typeof(XmlAttribute));
			LocalBuilder localBuilder8 = il.DeclareLocal(typeof(XmlAttribute));
			LocalBuilder localBuilder9 = il.DeclareLocal(typeof(string));
			LocalBuilder localBuilder10 = il.DeclareLocal(typeof(string));
			il.DeclareLocal(typeof(XmlAttribute));
			il.DeclareLocal(typeof(string));
			Label label = il.DefineLabel();
			Label label2 = il.DefineLabel();
			Label label3 = il.DefineLabel();
			Label label4 = il.DefineLabel();
			Label label5 = il.DefineLabel();
			Label label6 = il.DefineLabel();
			LocalBuilder localBuilder11 = il.DeclareLocal(typeof(Type));
			il.DefineLabel();
			Label label7 = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
			il.Emit(OpCodes.Ldstr, "IsNull");
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder6);
			il.Emit(OpCodes.Brfalse, label);
			il.Emit(OpCodes.Ldloc, localBuilder6);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
			il.Emit(OpCodes.Ldstr, "true");
			il.Emit(OpCodes.Ldc_I4, 3);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.StringEqualsWithComparisonModeMethod);
			il.Emit(OpCodes.Brfalse, label);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stloc, localBuilder);
			il.Emit(OpCodes.Br, label4);
			il.MarkLabel(label);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetChildNodesMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetCountMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder3);
			il.Emit(OpCodes.Newobj, constructor);
			il.Emit(OpCodes.Stloc, localBuilder);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stloc, localBuilder4);
			il.MarkLabel(label2);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Ldloc, localBuilder3);
			il.Emit(OpCodes.Bge, label4);
			il.Emit(OpCodes.Ldloc, localBuilder2);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetItemMethod);
			il.Emit(OpCodes.Stloc, localBuilder5);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldtoken, itemType);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.ValidateListNodeMethod);
			il.Emit(OpCodes.Brfalse, label3);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stloc, localBuilder9);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stloc, localBuilder10);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
			il.Emit(OpCodes.Ldstr, "MayRequire");
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder7);
			il.Emit(OpCodes.Brfalse, label5);
			il.Emit(OpCodes.Ldloc, localBuilder7);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
			il.Emit(OpCodes.Stloc, localBuilder9);
			il.MarkLabel(label5);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
			il.Emit(OpCodes.Ldstr, "MayRequireAnyOf");
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder8);
			il.Emit(OpCodes.Brfalse, label6);
			il.Emit(OpCodes.Ldloc, localBuilder8);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
			il.Emit(OpCodes.Stloc, localBuilder10);
			il.MarkLabel(label6);
			if (flag)
			{
				il.Emit(OpCodes.Ldloc, localBuilder);
				il.Emit(OpCodes.Castclass, listType);
				il.Emit(OpCodes.Ldloc, localBuilder5);
				il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetInnerTextMethod);
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetNameMethod);
				il.Emit(OpCodes.Box, typeof(string));
				il.Emit(OpCodes.Ldloc, localBuilder9);
				il.Emit(OpCodes.Ldloc, localBuilder10);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.RegisterListWantsCrossRefMethod.MakeGenericMethod(new Type[] { itemType }));
			}
			else
			{
				il.Emit(OpCodes.Ldloc, localBuilder9);
				il.Emit(OpCodes.Ldloc, localBuilder10);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.ValidateMayRequiresMethod);
				il.Emit(OpCodes.Brfalse, label3);
				il.Emit(OpCodes.Ldtoken, itemType);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
				il.Emit(OpCodes.Ldloc, localBuilder5);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.ResolveTypeForNodeFromTypeMethod);
				il.Emit(OpCodes.Stloc, localBuilder11);
				il.MarkLabel(label7);
				il.Emit(OpCodes.Ldloc, localBuilder11);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.GetListItemAdderForTypeMethod);
				il.Emit(OpCodes.Ldloc, localBuilder);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Ldloc, localBuilder5);
				il.Emit(OpCodes.Ldloc, localBuilder11);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.ParseValueAndAddListItemDelegateInvokeMethod);
			}
			il.MarkLabel(label3);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Stloc, localBuilder4);
			il.Emit(OpCodes.Br, label2);
			il.MarkLabel(label4);
			il.Emit(OpCodes.Ldloc, localBuilder);
		}

		// Token: 0x0600357D RID: 13693 RVA: 0x0011B16C File Offset: 0x0011936C
		private static void EmitILToCreateAndPopulateDictionary(ILGenerator il, Type dictionaryType, Type keyType, Type valueType)
		{
			bool flag = GenTypes.IsDef(keyType);
			bool flag2 = GenTypes.IsDef(valueType);
			bool flag3 = !flag && !flag2;
			Type type = typeof(KeyValuePair<, >).MakeGenericType(new Type[] { keyType, valueType });
			MethodInfo getMethod = type.GetProperty("Key").GetGetMethod();
			MethodInfo getMethod2 = type.GetProperty("Value").GetGetMethod();
			ConstructorInfo constructor = dictionaryType.GetConstructor(new Type[] { typeof(int) });
			LocalBuilder localBuilder = il.DeclareLocal(dictionaryType);
			LocalBuilder localBuilder2 = il.DeclareLocal(typeof(XmlNodeList));
			LocalBuilder localBuilder3 = il.DeclareLocal(typeof(int));
			LocalBuilder localBuilder4 = il.DeclareLocal(typeof(int));
			LocalBuilder localBuilder5 = il.DeclareLocal(typeof(XmlNode));
			LocalBuilder localBuilder6 = il.DeclareLocal(typeof(XmlNode));
			LocalBuilder localBuilder7 = il.DeclareLocal(typeof(XmlNode));
			LocalBuilder localBuilder8 = il.DeclareLocal(typeof(XmlAttribute));
			LocalBuilder localBuilder9 = il.DeclareLocal(typeof(DirectXmlToObjectNew.ParseValueAndSetFieldDelegate));
			LocalBuilder localBuilder10 = il.DeclareLocal(typeof(DirectXmlToObjectNew.ParseValueAndSetFieldDelegate));
			LocalBuilder localBuilder11 = il.DeclareLocal(type);
			LocalBuilder localBuilder12 = il.DeclareLocal(typeof(object));
			LocalBuilder localBuilder13 = il.DeclareLocal(typeof(FieldInfo));
			LocalBuilder localBuilder14 = il.DeclareLocal(typeof(FieldInfo));
			LocalBuilder localBuilder15 = il.DeclareLocal(typeof(Type));
			LocalBuilder localBuilder16 = il.DeclareLocal(typeof(Type));
			Label label = il.DefineLabel();
			Label label2 = il.DefineLabel();
			Label label3 = il.DefineLabel();
			Label label4 = il.DefineLabel();
			if (flag3)
			{
				il.Emit(OpCodes.Ldloca, localBuilder11);
				il.Emit(OpCodes.Initobj, type);
				il.Emit(OpCodes.Ldtoken, type);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
				il.Emit(OpCodes.Ldstr, "key");
				il.Emit(OpCodes.Ldc_I4, 36);
				il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.TypeGetFieldMethod);
				il.Emit(OpCodes.Stloc, localBuilder13);
				il.Emit(OpCodes.Ldtoken, type);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
				il.Emit(OpCodes.Ldstr, "value");
				il.Emit(OpCodes.Ldc_I4, 36);
				il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.TypeGetFieldMethod);
				il.Emit(OpCodes.Stloc, localBuilder14);
			}
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
			il.Emit(OpCodes.Ldstr, "IsNull");
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder8);
			il.Emit(OpCodes.Brfalse, label4);
			il.Emit(OpCodes.Ldloc, localBuilder8);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
			il.Emit(OpCodes.Ldstr, "true");
			il.Emit(OpCodes.Ldc_I4, 3);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.StringEqualsWithComparisonModeMethod);
			il.Emit(OpCodes.Brfalse, label4);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stloc, localBuilder);
			il.Emit(OpCodes.Br, label3);
			il.MarkLabel(label4);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetChildNodesMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetCountMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder3);
			il.Emit(OpCodes.Newobj, constructor);
			il.Emit(OpCodes.Stloc, localBuilder);
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stloc, localBuilder4);
			il.MarkLabel(label);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Ldloc, localBuilder3);
			il.Emit(OpCodes.Bge, label3);
			il.Emit(OpCodes.Ldloc, localBuilder2);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetItemMethod);
			il.Emit(OpCodes.Stloc, localBuilder5);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldtoken, type);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.ValidateListNodeMethod);
			il.Emit(OpCodes.Brfalse, label2);
			if (flag3)
			{
				il.Emit(OpCodes.Ldloc, localBuilder5);
				il.Emit(OpCodes.Ldstr, "key");
				il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetItemByNameMethod);
				il.Emit(OpCodes.Stloc, localBuilder6);
				il.Emit(OpCodes.Ldloc, localBuilder5);
				il.Emit(OpCodes.Ldstr, "value");
				il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetItemByNameMethod);
				il.Emit(OpCodes.Stloc, localBuilder7);
				il.Emit(OpCodes.Ldloc, localBuilder11);
				il.Emit(OpCodes.Box, type);
				il.Emit(OpCodes.Stloc, localBuilder12);
				il.Emit(OpCodes.Ldtoken, keyType);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
				il.Emit(OpCodes.Ldloc, localBuilder6);
				il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.ResolveTypeForNodeFromTypeMethod);
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Stloc, localBuilder15);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.GetFieldSetterForTypeMethod);
				il.Emit(OpCodes.Stloc, localBuilder9);
				il.Emit(OpCodes.Ldloc, localBuilder9);
				il.Emit(OpCodes.Ldloc, localBuilder12);
				il.Emit(OpCodes.Ldloc, localBuilder13);
				il.Emit(OpCodes.Ldloc, localBuilder6);
				il.Emit(OpCodes.Ldloc, localBuilder15);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.ParseValueAndSetFieldDelegateInvokeMethod);
				il.Emit(OpCodes.Ldtoken, valueType);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.TypeGetTypeFromHandleMethod);
				il.Emit(OpCodes.Ldloc, localBuilder7);
				il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.ResolveTypeForNodeFromTypeMethod);
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Stloc, localBuilder16);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.GetFieldSetterForTypeMethod);
				il.Emit(OpCodes.Stloc, localBuilder10);
				il.Emit(OpCodes.Ldloc, localBuilder10);
				il.Emit(OpCodes.Ldloc, localBuilder12);
				il.Emit(OpCodes.Ldloc, localBuilder14);
				il.Emit(OpCodes.Ldloc, localBuilder7);
				il.Emit(OpCodes.Ldloc, localBuilder16);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.ParseValueAndSetFieldDelegateInvokeMethod);
				il.Emit(OpCodes.Ldloc, localBuilder12);
				il.Emit(OpCodes.Unbox_Any, type);
				il.Emit(OpCodes.Stloc, localBuilder11);
				il.Emit(OpCodes.Ldloc, localBuilder);
				il.Emit(OpCodes.Ldloca, localBuilder11);
				il.Emit(OpCodes.Call, getMethod);
				if (keyType.IsValueType)
				{
					il.Emit(OpCodes.Box, keyType);
				}
				il.Emit(OpCodes.Ldloca, localBuilder11);
				il.Emit(OpCodes.Call, getMethod2);
				if (valueType.IsValueType)
				{
					il.Emit(OpCodes.Box, valueType);
				}
				il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.DictionaryAddMethod);
			}
			else
			{
				MethodInfo methodInfo = DirectXmlToObjectNew.RegisterDictionaryWantsCrossRefMethod.MakeGenericMethod(new Type[] { keyType, valueType });
				il.Emit(OpCodes.Ldloc, localBuilder);
				il.Emit(OpCodes.Ldloc, localBuilder5);
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetNameMethod);
				il.Emit(OpCodes.Call, methodInfo);
			}
			il.MarkLabel(label2);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Stloc, localBuilder4);
			il.Emit(OpCodes.Br, label);
			il.MarkLabel(label3);
			il.Emit(OpCodes.Ldloc, localBuilder);
		}

		// Token: 0x0600357E RID: 13694 RVA: 0x0011B980 File Offset: 0x00119B80
		private static void EmitIlToCreateAndPopulateComplexType(ILGenerator il, Type typeBeingDeserialized, LocalBuilder instantiatedObjectLocal)
		{
			LocalBuilder localBuilder = il.DeclareLocal(typeof(XmlNodeList));
			LocalBuilder localBuilder2 = il.DeclareLocal(typeof(int));
			LocalBuilder localBuilder3 = il.DeclareLocal(typeof(int));
			LocalBuilder localBuilder4 = il.DeclareLocal(typeof(XmlNode));
			LocalBuilder localBuilder5 = il.DeclareLocal(typeof(FieldInfo));
			LocalBuilder localBuilder6 = il.DeclareLocal(typeof(Type));
			LocalBuilder localBuilder7 = il.DeclareLocal(typeof(object));
			LocalBuilder localBuilder8 = il.DeclareLocal(typeof(XmlAttribute));
			LocalBuilder localBuilder9 = il.DeclareLocal(typeof(XmlAttribute));
			LocalBuilder localBuilder10 = il.DeclareLocal(typeof(string));
			LocalBuilder localBuilder11 = il.DeclareLocal(typeof(string));
			Label label = il.DefineLabel();
			Label label2 = il.DefineLabel();
			Label label3 = il.DefineLabel();
			Label label4 = il.DefineLabel();
			Label label5 = il.DefineLabel();
			Label label6 = il.DefineLabel();
			Label label7 = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.XmlInheritanceGetResolvedNodeForMethod);
			il.Emit(OpCodes.Starg_S, 2);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetChildNodesMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetCountMethod);
			il.Emit(OpCodes.Stloc, localBuilder2);
			if (typeBeingDeserialized.IsValueType)
			{
				il.Emit(OpCodes.Ldloca, instantiatedObjectLocal);
				il.Emit(OpCodes.Initobj, typeBeingDeserialized);
			}
			else
			{
				il.Emit(OpCodes.Ldarg_3);
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Call, DirectXmlToObjectNew.MakeInstanceOfTypeForEmptyNodeMethod);
				il.Emit(OpCodes.Stloc, instantiatedObjectLocal);
			}
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Stloc, localBuilder3);
			il.MarkLabel(label);
			il.Emit(OpCodes.Ldloc, localBuilder3);
			il.Emit(OpCodes.Ldloc, localBuilder2);
			il.Emit(OpCodes.Bge, label3);
			if (typeBeingDeserialized.IsValueType)
			{
				il.Emit(OpCodes.Ldloc, instantiatedObjectLocal);
				il.Emit(OpCodes.Box, typeBeingDeserialized);
				il.Emit(OpCodes.Stloc, localBuilder7);
			}
			il.Emit(OpCodes.Ldloc, localBuilder);
			il.Emit(OpCodes.Ldloc, localBuilder3);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeListGetItemMethod);
			il.Emit(OpCodes.Stloc, localBuilder4);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Isinst, typeof(XmlComment));
			il.Emit(OpCodes.Brtrue, label2);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.ResolveFieldForNodeMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder5);
			il.Emit(OpCodes.Brfalse, label2);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.ResolveTypeForNodeFromFieldMethod);
			il.Emit(OpCodes.Stloc, localBuilder6);
			il.Emit(OpCodes.Ldloc, localBuilder6);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.GenTypesIsDefMethod);
			il.Emit(OpCodes.Brfalse, label4);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetInnerTextMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.GenTextIsNullOrEmptyMethod);
			il.Emit(OpCodes.Brfalse, label5);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Ldloc, typeBeingDeserialized.IsValueType ? localBuilder7 : instantiatedObjectLocal);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.FieldSetValueMethod);
			il.Emit(OpCodes.Br, label2);
			il.MarkLabel(label5);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stloc, localBuilder10);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stloc, localBuilder11);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
			il.Emit(OpCodes.Ldstr, "MayRequire");
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder8);
			il.Emit(OpCodes.Brfalse, label6);
			il.Emit(OpCodes.Ldloc, localBuilder8);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.StringToLowerMethod);
			il.Emit(OpCodes.Stloc, localBuilder10);
			il.MarkLabel(label6);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
			il.Emit(OpCodes.Ldstr, "MayRequireAnyOf");
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder9);
			il.Emit(OpCodes.Brfalse, label7);
			il.Emit(OpCodes.Ldloc, localBuilder9);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.StringToLowerMethod);
			il.Emit(OpCodes.Stloc, localBuilder11);
			il.MarkLabel(label7);
			il.Emit(OpCodes.Ldloc, typeBeingDeserialized.IsValueType ? localBuilder7 : instantiatedObjectLocal);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetInnerTextMethod);
			il.Emit(OpCodes.Ldloc, localBuilder10);
			il.Emit(OpCodes.Ldloc, localBuilder11);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.RegisterObjectWantsCrossRefMethod);
			il.Emit(OpCodes.Br, label2);
			il.MarkLabel(label4);
			il.Emit(OpCodes.Ldloc, localBuilder6);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.GetFieldSetterForTypeMethod);
			il.Emit(OpCodes.Ldloc, typeBeingDeserialized.IsValueType ? localBuilder7 : instantiatedObjectLocal);
			il.Emit(OpCodes.Ldloc, localBuilder5);
			il.Emit(OpCodes.Ldloc, localBuilder4);
			il.Emit(OpCodes.Ldloc, localBuilder6);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.ParseValueAndSetFieldDelegateInvokeMethod);
			il.MarkLabel(label2);
			if (typeBeingDeserialized.IsValueType)
			{
				il.Emit(OpCodes.Ldloc, localBuilder7);
				il.Emit(OpCodes.Unbox_Any, typeBeingDeserialized);
				il.Emit(OpCodes.Stloc, instantiatedObjectLocal);
			}
			il.Emit(OpCodes.Ldloc, localBuilder3);
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Add);
			il.Emit(OpCodes.Stloc, localBuilder3);
			il.Emit(OpCodes.Br, label);
			il.MarkLabel(label3);
			MethodInfo methodInfo = XmlToObjectUtils.PostLoadMethodOf(typeBeingDeserialized);
			if (methodInfo != null)
			{
				il.Emit(OpCodes.Ldloc, instantiatedObjectLocal);
				il.Emit(OpCodes.Call, methodInfo);
				return;
			}
			il.Emit(OpCodes.Nop);
		}

		// Token: 0x0600357F RID: 13695 RVA: 0x0011C080 File Offset: 0x0011A280
		private static void EmitIlToHandleEmptyNode(ILGenerator il, Type typeBeingDeserialized, LocalBuilder localForResultValue, Label labelToJumpIfNotEmpty, Label endLabel)
		{
			LocalBuilder localBuilder = il.DeclareLocal(typeof(XmlAttribute));
			Label label = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetAttributesMethod);
			il.Emit(OpCodes.Ldstr, "IsNull");
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeCollectionGetItemByNameMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder);
			il.Emit(OpCodes.Brfalse, label);
			il.Emit(OpCodes.Ldloc, localBuilder);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlAttributeGetValueMethod);
			il.Emit(OpCodes.Ldstr, "true");
			il.Emit(OpCodes.Ldc_I4, 3);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.StringEqualsWithComparisonModeMethod);
			il.Emit(OpCodes.Brfalse, label);
			if (typeBeingDeserialized.IsValueType)
			{
				il.Emit(OpCodes.Ldloca, localForResultValue);
				il.Emit(OpCodes.Initobj, typeBeingDeserialized);
			}
			else
			{
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Stloc, localForResultValue);
			}
			il.Emit(OpCodes.Br, endLabel);
			il.MarkLabel(label);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeHasChildNodesMethod);
			il.Emit(OpCodes.Brtrue, labelToJumpIfNotEmpty);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.MakeInstanceOfTypeForEmptyNodeMethod);
			if (typeBeingDeserialized.IsValueType)
			{
				il.Emit(OpCodes.Unbox_Any, typeBeingDeserialized);
			}
			il.Emit(OpCodes.Stloc, localForResultValue);
			il.Emit(OpCodes.Br, endLabel);
		}

		// Token: 0x06003580 RID: 13696 RVA: 0x0011C220 File Offset: 0x0011A420
		private static void EmitIlToErrorAndMakeDefaultValueForCdata(ILGenerator il, Type typeBeingDeserialized, Label labelToJumpIfNotCdata, Label labelToJumpAfterCheck, LocalBuilder localForResultValue)
		{
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetFirstChildMethod);
			il.Emit(OpCodes.Brfalse, labelToJumpIfNotCdata);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetFirstChildMethod);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetNodeTypeMethod);
			il.Emit(OpCodes.Ldc_I4, 4);
			il.Emit(OpCodes.Bne_Un, labelToJumpIfNotCdata);
			il.Emit(OpCodes.Ldstr, "CDATA can only be used for strings. Bad xml: ");
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetOuterXmlMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.StringConcatMethod);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.LogErrorMethod);
			if (typeBeingDeserialized.IsValueType)
			{
				il.Emit(OpCodes.Ldloca_S, localForResultValue);
				il.Emit(OpCodes.Initobj, typeBeingDeserialized);
			}
			else
			{
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Stloc, localForResultValue);
			}
			il.Emit(OpCodes.Br, labelToJumpAfterCheck);
		}

		// Token: 0x06003581 RID: 13697 RVA: 0x0011C32C File Offset: 0x0011A52C
		private static void EmitIlToHandleSingleTextNodeViaParseHelper(ILGenerator il, Type typeBeingDeserialized, LocalBuilder localForResultValue, Label labelToJumpIfNotText, Label endLabel)
		{
			LocalBuilder localBuilder = il.DeclareLocal(typeof(XmlNode));
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.GetNodeOnlyChildMethod);
			il.Emit(OpCodes.Dup);
			il.Emit(OpCodes.Stloc, localBuilder);
			il.Emit(OpCodes.Brfalse, labelToJumpIfNotText);
			il.Emit(OpCodes.Ldloc, localBuilder);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetNodeTypeMethod);
			il.Emit(OpCodes.Ldc_I4, 3);
			il.Emit(OpCodes.Bne_Un, labelToJumpIfNotText);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Callvirt, DirectXmlToObjectNew.XmlNodeGetInnerTextMethod);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Call, DirectXmlToObjectNew.ParseHelperFromStringNonGenericMethod);
			if (typeBeingDeserialized.IsValueType)
			{
				il.Emit(OpCodes.Unbox_Any, typeBeingDeserialized);
				il.Emit(OpCodes.Stloc, localForResultValue);
			}
			else
			{
				il.Emit(OpCodes.Castclass, typeBeingDeserialized);
				il.Emit(OpCodes.Stloc, localForResultValue);
			}
			il.Emit(OpCodes.Br, endLabel);
		}

		// Token: 0x06003582 RID: 13698 RVA: 0x0011C439 File Offset: 0x0011A639
		public static FieldInfo ResolveFieldForNode(Type typeBeingDeserialized, XmlNode node, XmlNode parentForDebug)
		{
			return XmlToObjectUtils.DoFieldSearch(typeBeingDeserialized.GetInnerIfNullable(), node, parentForDebug);
		}

		// Token: 0x06003583 RID: 13699 RVA: 0x0011C448 File Offset: 0x0011A648
		public static Type ResolveTypeForNode(FieldInfo fieldBeingSet, XmlNode node)
		{
			return DirectXmlToObjectNew.ResolveTypeForNode(fieldBeingSet.FieldType, node);
		}

		// Token: 0x06003584 RID: 13700 RVA: 0x0011C458 File Offset: 0x0011A658
		public static Type ResolveTypeForNode(Type defaultType, XmlNode node)
		{
			XmlAttributeCollection attributes = node.Attributes;
			XmlAttribute xmlAttribute = ((attributes != null) ? attributes["Class"] : null);
			if (xmlAttribute == null)
			{
				return defaultType;
			}
			Type typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(xmlAttribute.Value, defaultType.GetInnerIfNullable().Namespace);
			if (typeInAnyAssembly != null)
			{
				return typeInAnyAssembly;
			}
			throw new ArgumentException("Could not find type named " + xmlAttribute.Value + " from node " + node.OuterXml, "node");
		}

		// Token: 0x06003585 RID: 13701 RVA: 0x0011C4CC File Offset: 0x0011A6CC
		public static XmlNode GetNodeOnlyChild(XmlNode node)
		{
			XmlNode firstChild = node.FirstChild;
			if (firstChild == null || firstChild.NextSibling != null)
			{
				return null;
			}
			return firstChild;
		}

		// Token: 0x06003586 RID: 13702 RVA: 0x0011C4F0 File Offset: 0x0011A6F0
		public static object MakeInstanceOfTypeForEmptyNode(Type type, XmlNode node)
		{
			type = DirectXmlToObjectNew.ResolveTypeForNode(type, node);
			if (type.IsValueType)
			{
				return RuntimeHelpers.GetUninitializedObject(type);
			}
			object obj;
			try
			{
				obj = Activator.CreateInstance(type);
			}
			catch (MissingMethodException ex)
			{
				throw new InvalidOperationException(string.Concat(new string[] { "Cannot deserialize XML node ", node.OuterXml, " to type ", type.FullName, " due to missing parameterless constructor." }), ex);
			}
			return obj;
		}

		// Token: 0x06003587 RID: 13703 RVA: 0x0011C570 File Offset: 0x0011A770
		public static bool ValidateMayRequires(string mayRequire, string mayRequireAny)
		{
			if (!mayRequire.NullOrEmpty() && !ModLister.AllModsActiveNoSuffix(mayRequire.Split(',', StringSplitOptions.None)))
			{
				if (DirectXmlCrossRefLoader.MistypedMayRequire(mayRequire))
				{
					Log.Error("Faulty MayRequire: " + mayRequire);
				}
				return false;
			}
			return mayRequireAny.NullOrEmpty() || ModLister.AnyModActiveNoSuffix(mayRequireAny.Split(',', StringSplitOptions.None));
		}

		// Token: 0x06003588 RID: 13704 RVA: 0x0011C5CA File Offset: 0x0011A7CA
		private static string GetDynamicMethodNameSuffixForType(Type type)
		{
			string fullName = type.FullName;
			return ((fullName != null) ? fullName.Replace('.', '_') : null) ?? "UnknownType";
		}

		// Token: 0x06003589 RID: 13705 RVA: 0x0011C5EB File Offset: 0x0011A7EB
		private static Type GetInnerIfNullable(this Type type)
		{
			return Nullable.GetUnderlyingType(type) ?? type;
		}

		// Token: 0x0600358A RID: 13706 RVA: 0x0011C5F8 File Offset: 0x0011A7F8
		private static bool TypeCanBeDeserializedWithSharedBody(Type type)
		{
			if (type.IsValueType)
			{
				return false;
			}
			if (type.IsGenericType || type.IsConstructedGenericType)
			{
				return false;
			}
			if (XmlToObjectUtils.CustomDataLoadMethodOf(type) != null)
			{
				return false;
			}
			MethodInfo methodInfo = XmlToObjectUtils.PostLoadMethodOf(type);
			return (methodInfo == null || !(methodInfo.DeclaringType != typeof(Editable))) && !ParseHelper.HandlesType(type);
		}

		// Token: 0x04002850 RID: 10320
		private static readonly MethodInfo XmlNodeGetChildNodesMethod = typeof(XmlNode).GetProperty("ChildNodes").GetGetMethod();

		// Token: 0x04002851 RID: 10321
		private static readonly MethodInfo XmlNodeGetInnerTextMethod = typeof(XmlNode).GetProperty("InnerText").GetGetMethod();

		// Token: 0x04002852 RID: 10322
		private static readonly MethodInfo XmlNodeGetFirstChildMethod = typeof(XmlNode).GetProperty("FirstChild").GetGetMethod();

		// Token: 0x04002853 RID: 10323
		private static readonly MethodInfo XmlNodeHasChildNodesMethod = typeof(XmlNode).GetProperty("HasChildNodes").GetGetMethod();

		// Token: 0x04002854 RID: 10324
		private static readonly MethodInfo XmlNodeGetOuterXmlMethod = typeof(XmlNode).GetProperty("OuterXml").GetGetMethod();

		// Token: 0x04002855 RID: 10325
		private static readonly MethodInfo XmlNodeGetNodeTypeMethod = typeof(XmlNode).GetProperty("NodeType").GetGetMethod();

		// Token: 0x04002856 RID: 10326
		private static readonly MethodInfo XmlNodeGetValueMethod = typeof(XmlNode).GetProperty("Value").GetGetMethod();

		// Token: 0x04002857 RID: 10327
		private static readonly MethodInfo XmlNodeGetNameMethod = typeof(XmlNode).GetProperty("Name").GetGetMethod();

		// Token: 0x04002858 RID: 10328
		private static readonly MethodInfo XmlNodeGetAttributesMethod = typeof(XmlNode).GetProperty("Attributes").GetGetMethod();

		// Token: 0x04002859 RID: 10329
		private static readonly MethodInfo XmlNodeGetItemByNameMethod = typeof(XmlNode).GetMethod("get_Item", new Type[] { typeof(string) });

		// Token: 0x0400285A RID: 10330
		private static readonly MethodInfo XmlNodeListGetCountMethod = typeof(XmlNodeList).GetProperty("Count").GetGetMethod();

		// Token: 0x0400285B RID: 10331
		private static readonly MethodInfo XmlNodeListGetItemMethod = typeof(XmlNodeList).GetMethod("Item", new Type[] { typeof(int) });

		// Token: 0x0400285C RID: 10332
		private static readonly MethodInfo XmlAttributeCollectionGetItemByNameMethod = typeof(XmlAttributeCollection).GetMethod("get_ItemOf", new Type[] { typeof(string) });

		// Token: 0x0400285D RID: 10333
		private static readonly MethodInfo XmlAttributeGetValueMethod = typeof(XmlAttribute).GetProperty("Value").GetGetMethod();

		// Token: 0x0400285E RID: 10334
		private static readonly MethodInfo FieldSetValueMethod = typeof(FieldInfo).GetMethod("SetValue", new Type[]
		{
			typeof(object),
			typeof(object)
		});

		// Token: 0x0400285F RID: 10335
		private static readonly MethodInfo IListAddItemMethod = typeof(IList).GetMethod("Add", new Type[] { typeof(object) });

		// Token: 0x04002860 RID: 10336
		private static readonly MethodInfo ValidateListNodeMethod = typeof(DirectXmlToObject).GetMethod("ValidateListNode", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x04002861 RID: 10337
		private static readonly MethodInfo ParseHelperFromStringNonGenericMethod = typeof(ParseHelper).GetMethod("FromString", BindingFlags.Static | BindingFlags.Public, null, new Type[]
		{
			typeof(string),
			typeof(Type)
		}, null);

		// Token: 0x04002862 RID: 10338
		private static readonly MethodInfo InnerTextWithReplacedNewlinesOrXmlMethod = typeof(DirectXmlToObject).GetMethod("InnerTextWithReplacedNewlinesOrXML", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x04002863 RID: 10339
		private static readonly MethodInfo LogMessageMethod = typeof(Log).GetMethod("Message", new Type[] { typeof(string) });

		// Token: 0x04002864 RID: 10340
		private static readonly MethodInfo LogErrorMethod = typeof(Log).GetMethod("Error", new Type[] { typeof(string) });

		// Token: 0x04002865 RID: 10341
		private static readonly MethodInfo TypeGetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle");

		// Token: 0x04002866 RID: 10342
		private static readonly MethodInfo TypeGetFieldMethod = typeof(Type).GetMethod("GetField", new Type[]
		{
			typeof(string),
			typeof(BindingFlags)
		});

		// Token: 0x04002867 RID: 10343
		private static readonly MethodInfo StringConcatMethod = typeof(string).GetMethod("Concat", new Type[]
		{
			typeof(string),
			typeof(string)
		});

		// Token: 0x04002868 RID: 10344
		private static readonly MethodInfo StringConcatObjObjMethod = typeof(string).GetMethod("Concat", new Type[]
		{
			typeof(object),
			typeof(object)
		});

		// Token: 0x04002869 RID: 10345
		private static readonly MethodInfo StringToLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);

		// Token: 0x0400286A RID: 10346
		private static readonly MethodInfo StringEqualsWithComparisonModeMethod = typeof(string).GetMethod("Equals", new Type[]
		{
			typeof(string),
			typeof(StringComparison)
		});

		// Token: 0x0400286B RID: 10347
		private static readonly MethodInfo XmlInheritanceGetResolvedNodeForMethod = typeof(XmlInheritance).GetMethod("GetResolvedNodeFor", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x0400286C RID: 10348
		private static readonly MethodInfo DictionaryAddMethod = typeof(IDictionary).GetMethod("Add");

		// Token: 0x0400286D RID: 10349
		private static readonly MethodInfo RegisterObjectWantsCrossRefMethod = typeof(DirectXmlCrossRefLoader).GetMethod("RegisterObjectWantsCrossRef", BindingFlags.Static | BindingFlags.Public, null, new Type[]
		{
			typeof(object),
			typeof(FieldInfo),
			typeof(string),
			typeof(string),
			typeof(string),
			typeof(Type)
		}, null);

		// Token: 0x0400286E RID: 10350
		private static readonly MethodInfo RegisterDictionaryWantsCrossRefMethod = typeof(DirectXmlCrossRefLoader).GetMethods(BindingFlags.Static | BindingFlags.Public).First((MethodInfo m) => m.Name == "RegisterDictionaryWantsCrossRef");

		// Token: 0x0400286F RID: 10351
		private static readonly MethodInfo RegisterListWantsCrossRefMethod = typeof(DirectXmlCrossRefLoader).GetMethods(BindingFlags.Static | BindingFlags.Public).First((MethodInfo m) => m.Name == "RegisterListWantsCrossRef");

		// Token: 0x04002870 RID: 10352
		private static readonly MethodInfo GenTypesIsDefMethod = typeof(GenTypes).GetMethod("IsDef", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x04002871 RID: 10353
		private static readonly MethodInfo GenTypesGetTypeInAnyAssemblyMethod = typeof(GenTypes).GetMethod("GetTypeInAnyAssembly", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x04002872 RID: 10354
		private static readonly MethodInfo GenTextIsNullOrEmptyMethod = typeof(GenText).GetMethod("NullOrEmpty", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x04002873 RID: 10355
		private static readonly MethodInfo GetFieldSetterForTypeMethod = typeof(DirectXmlToObjectNew).GetMethod("GetFieldSetterForType", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x04002874 RID: 10356
		private static readonly MethodInfo GetListItemAdderForTypeMethod = typeof(DirectXmlToObjectNew).GetMethod("GetListItemAdderForType", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x04002875 RID: 10357
		private static readonly MethodInfo ResolveFieldForNodeMethod = typeof(DirectXmlToObjectNew).GetMethod("ResolveFieldForNode", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x04002876 RID: 10358
		private static readonly MethodInfo ResolveTypeForNodeFromFieldMethod = typeof(DirectXmlToObjectNew).GetMethod("ResolveTypeForNode", BindingFlags.Static | BindingFlags.Public, null, new Type[]
		{
			typeof(FieldInfo),
			typeof(XmlNode)
		}, null);

		// Token: 0x04002877 RID: 10359
		private static readonly MethodInfo ResolveTypeForNodeFromTypeMethod = typeof(DirectXmlToObjectNew).GetMethod("ResolveTypeForNode", BindingFlags.Static | BindingFlags.Public, null, new Type[]
		{
			typeof(Type),
			typeof(XmlNode)
		}, null);

		// Token: 0x04002878 RID: 10360
		private static readonly MethodInfo MakeInstanceOfTypeForEmptyNodeMethod = typeof(DirectXmlToObjectNew).GetMethod("MakeInstanceOfTypeForEmptyNode", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x04002879 RID: 10361
		private static readonly MethodInfo GetNodeOnlyChildMethod = typeof(DirectXmlToObjectNew).GetMethod("GetNodeOnlyChild", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x0400287A RID: 10362
		private static readonly MethodInfo ValidateMayRequiresMethod = typeof(DirectXmlToObjectNew).GetMethod("ValidateMayRequires", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x0400287B RID: 10363
		private static readonly MethodInfo ParseValueAndSetFieldDelegateInvokeMethod = typeof(DirectXmlToObjectNew.ParseValueAndSetFieldDelegate).GetMethod("Invoke");

		// Token: 0x0400287C RID: 10364
		private static readonly MethodInfo ParseValueAndAddListItemDelegateInvokeMethod = typeof(DirectXmlToObjectNew.ParseValueAndAddListItemDelegate).GetMethod("Invoke");

		// Token: 0x0400287D RID: 10365
		private static readonly MethodInfo ParseValueAndReturnDefDelegateInvokeMethod = typeof(DirectXmlToObjectNew.ParseValueAndReturnDefDelegate).GetMethod("Invoke");

		// Token: 0x0400287E RID: 10366
		private static readonly ConstructorInfo InvalidOperationExceptionStringConstructor = typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) });

		// Token: 0x0400287F RID: 10367
		private static readonly Dictionary<Type, DirectXmlToObjectNew.ParseValueAndSetFieldDelegate> parseMethods = new Dictionary<Type, DirectXmlToObjectNew.ParseValueAndSetFieldDelegate>();

		// Token: 0x04002880 RID: 10368
		private static readonly Dictionary<Type, DirectXmlToObjectNew.ParseValueAndAddListItemDelegate> parseListItemMethods = new Dictionary<Type, DirectXmlToObjectNew.ParseValueAndAddListItemDelegate>();

		// Token: 0x04002881 RID: 10369
		private static readonly Dictionary<Type, DirectXmlToObjectNew.ParseValueAndReturnDefDelegate> parseDefMethods = new Dictionary<Type, DirectXmlToObjectNew.ParseValueAndReturnDefDelegate>();

		// Token: 0x0200083C RID: 2108
		// (Invoke) Token: 0x0600358D RID: 13709
		public delegate void ParseValueAndSetFieldDelegate(object target, FieldInfo field, XmlNode node, Type typeBeingDeserialized);

		// Token: 0x0200083D RID: 2109
		// (Invoke) Token: 0x06003591 RID: 13713
		public delegate void ParseValueAndAddListItemDelegate(IList target, int unused, XmlNode node, Type itemType);

		// Token: 0x0200083E RID: 2110
		// (Invoke) Token: 0x06003595 RID: 13717
		public delegate Def ParseValueAndReturnDefDelegate(int unused, int unused2, XmlNode node, Type defType);

		// Token: 0x0200083F RID: 2111
		private class DummyTypeToHoldDynamicMethods
		{
		}
	}
}
