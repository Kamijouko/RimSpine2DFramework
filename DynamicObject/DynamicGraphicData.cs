using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace DynamicObject
{
    public class DynamicGraphicData : GraphicData
    {
		public string shaderName = "Spine/Skeleton";

		public Graphic DynamicGraphic
        {
            get
            {
				Traverse data = Traverse.Create(this).Field("cachedGraphic");
				if ((Graphic)data.GetValue() == null)
				{
					this.SpecialInit();
				}
				return (Graphic)data.GetValue();
			}
        }

        public void SpecialInit()
        {
			Traverse data = Traverse.Create(this).Field("cachedGraphic");
			if (this.graphicClass == null || !ModStaticMethod.AllLevelsLoaded)
			{
				data.SetValue(null);
				return;
			}
			ShaderTypeDef cutout = this.shaderType;
			if (cutout == null)
			{
				cutout = ShaderTypeDefOf.Cutout;
			}
			Shader shader = ModDynamicObjectManager.spineShaderDatabase.ContainsKey(shaderName) ? ModDynamicObjectManager.spineShaderDatabase[shaderName] : cutout.Shader;
			data.SetValue(GraphicDatabase.Get(this.graphicClass, this.texPath, shader, this.drawSize, this.color, this.colorTwo, this, this.shaderParameters, this.maskPath));
			if (this.onGroundRandomRotateAngle > 0.01f)
			{
				data.SetValue(new Graphic_RandomRotated((Graphic)data.GetValue(), this.onGroundRandomRotateAngle));
			}
			if (this.Linked)
			{
				data.SetValue(GraphicUtility.WrapLinked((Graphic)data.GetValue(), this.linkType));
			}
		}
    }
}
