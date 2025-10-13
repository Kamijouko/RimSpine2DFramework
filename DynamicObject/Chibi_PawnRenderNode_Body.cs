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
            if (props.texPath.NullOrEmpty())
            {
                return null;
            }
            Shader shader = base.ShaderFor(pawn);
            if (shader == null)
            {
                return null;
            }
            return GraphicDatabase.Get<Graphic_Single>(props.texPath, shader, Vector2.one, this.ColorFor(pawn));
            /*if (pawn.story.bodyType == BodyTypeDefOf.Thin)
            {
                pawn.story.bodyType = pawn.story.Childhood.bodyTypeFemale;
            }
            if (pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated)
            {
                return GraphicDatabase.Get<Graphic_Single>(pawn.story.bodyType.bodyDessicatedGraphicPath, shader);
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
            return GraphicDatabase.Get<Graphic_Single>(pawn.story.bodyType.bodyNakedGraphicPath, shader, Vector2.one, this.ColorFor(pawn));*/
        }
    }
}
