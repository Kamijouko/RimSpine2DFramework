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
        public Chibi_PawnRenderNode_Body(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {

        }

        public override Graphic GraphicFor(Pawn pawn)
        {
            Shader shader = base.ShaderFor(pawn);
            if (shader == null)
            {
                return null;
            }
            if (pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated)
            {
                return GraphicDatabase.Get<Graphic_Single>(pawn.story.bodyType.bodyDessicatedGraphicPath, shader);
            }
            if (pawn.IsMutant && !pawn.mutant.Def.bodyTypeGraphicPaths.NullOrEmpty<BodyTypeGraphicData>())
            {
                string bodyGraphicPath = pawn.mutant.Def.GetBodyGraphicPath(pawn);
                if (bodyGraphicPath != null)
                {
                    return GraphicDatabase.Get<Graphic_Single>(bodyGraphicPath, shader, Vector2.one, this.ColorFor(pawn));
                }
            }
            if (ModsConfig.AnomalyActive && pawn.IsCreepJoiner && pawn.story.bodyType != null && !pawn.creepjoiner.form.bodyTypeGraphicPaths.NullOrEmpty<BodyTypeGraphicData>())
            {
                return GraphicDatabase.Get<Graphic_Single>(pawn.creepjoiner.form.GetBodyGraphicPath(pawn), shader, Vector2.one, this.ColorFor(pawn));
            }
            Pawn_StoryTracker story = pawn.story;
            bool flag;
            if (story == null)
            {
                flag = null != null;
            }
            else
            {
                BodyTypeDef bodyType = story.bodyType;
                flag = ((bodyType != null) ? bodyType.bodyNakedGraphicPath : null) != null;
            }
            if (!flag)
            {
                return null;
            }
            return GraphicDatabase.Get<Graphic_Single>(pawn.story.bodyType.bodyNakedGraphicPath, shader, Vector2.one, this.ColorFor(pawn));
        }
    }
}
