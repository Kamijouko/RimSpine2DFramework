using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace DynamicObject
{
    public class DynamicStoryTellerDef : Def
    {
        public StorytellerDef storyTeller;

        public DynamicObjectDef dynamicObject;

        public string animationName;

        public string skin = "default";

        public bool loop = true;
    }
}
