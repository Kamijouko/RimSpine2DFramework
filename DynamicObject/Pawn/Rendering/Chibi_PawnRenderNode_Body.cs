using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimSpine2DFramework
{
    public class Chibi_PawnRenderNode_Body : PawnRenderNode_Body
    {
        private readonly string initialTexPath;

        public Chibi_PawnRenderNode_Body(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
            initialTexPath = props?.texPath;
        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            if (TryResolveChibiGraphic(pawn, out Graphic graphic))
            {
                return graphic;
            }

            return base.GraphicFor(pawn);
        }

        internal bool TryResolveChibiGraphic(Pawn pawn, out Graphic graphic)
        {
            graphic = null;

            if (pawn == null || props == null)
            {
                return false;
            }

            string texPath = initialTexPath ?? props.texPath;
            if (texPath.NullOrEmpty())
            {
                return false;
            }

            // AlienRace 兼容：其前置补丁会把 texPath 改写为目录结构（例如 "Things/Pawn/Humanlike/Bodies/"）。
            // 保留构造时的路径副本，遇到目录路径时回落到原始配置的贴图，避免错误地交给 Graphic_Multi。
            if (texPath.EndsWith("/", StringComparison.Ordinal) || texPath.EndsWith("\\", StringComparison.Ordinal))
            {
                return false;
            }

            Shader shader = ShaderFor(pawn);
            if (shader == null)
            {
                return false;
            }

            try
            {
                graphic = GraphicDatabase.Get<Graphic_Single>(texPath, shader, Vector2.one, ColorFor(pawn));
                return graphic != null;
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"[RimSpine2DFramework] Failed to load chibi body graphic at '{texPath}': {ex}", texPath.GetHashCode() ^ GetHashCode());
                return false;
            }
        }
    }
}
