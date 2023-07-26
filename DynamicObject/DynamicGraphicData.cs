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
		public string shaderName = "Spine40/Skeleton";
		public Graphic tempGraph = null;

		public Graphic DynamicGraphic
        {
            get
            {
				if (tempGraph == null)
				{
					this.SpecialInit();
				}
				return tempGraph;
			}
        }

        public void SpecialInit()
        {
			if (this.graphicClass == null)
			{
				tempGraph = null;
				return;
			}
			ShaderTypeDef cutout = this.shaderType;
			if (cutout == null)
			{
				cutout = ShaderTypeDefOf.Cutout;
			}
			Shader shader = ModDynamicObjectManager.spineShaderDatabase.ContainsKey(shaderName) ? ModDynamicObjectManager.spineShaderDatabase[shaderName] : cutout.Shader;
			tempGraph = GraphicDatabase.Get(this.graphicClass, this.texPath, shader, this.drawSize, this.color, this.colorTwo, this, this.shaderParameters, this.maskPath);
			if (this.onGroundRandomRotateAngle > 0.01f)
			{
				tempGraph = new Graphic_RandomRotated(tempGraph, this.onGroundRandomRotateAngle);
			}
			if (this.Linked)
			{
				tempGraph = GraphicUtility.WrapLinked(tempGraph, this.linkType);
			}
		}
    }
}
