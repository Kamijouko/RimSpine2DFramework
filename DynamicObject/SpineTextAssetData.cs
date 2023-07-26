using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RimSpine2DFramework
{
    public class SpineTextAssetData
    {
        public TextAsset atlasTxt;
        public TextAsset skeletonByte;

        public Material[] materials;
        public Texture2D[] textures;
        public Shader shader;

        public SpineTextAssetData(TextAsset atlas, TextAsset skeleton, Material[] mats = null, Texture2D[] texs = null, Shader shade = null)
        {
            atlasTxt = atlas;
            skeletonByte = skeleton;
            materials = mats;
            textures = texs;
            shader = shade;
        }
    }
}
