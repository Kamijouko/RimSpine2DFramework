using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace RimSpine2DFramework
{
    public class DynamicPawnComp : ThingComp
    {
        public DynamicPawnComp_Properties Props { get { return (DynamicPawnComp_Properties)props; } }

        public GameObject CurDynamicObject { get { return curDynamicObject; } }

        private GameObject curDynamicObject;

        protected Pawn PawnOwner
        {
            get
            {
                Pawn result;
                if ((result = (parent as Pawn)) != null)
                {
                    return result;
                }
                return null;
            }
        }

        public DynamicPawnComp() 
        { 

        }

        public void CreateDynamicObject()
        {
            DynamicPawnStateRegistry.TryCreateAndBindInstancesForPawn(PawnOwner.kindDef, PawnOwner, out GameObject obj);
            if (obj != null)
            {
                curDynamicObject = obj;
            }
        }

        private bool check = true;

        public override void CompTick()
        {
            if (check)
            {
                //base.PostSpawnSetup(respawningAfterLoad);
                if (curDynamicObject != null)
                {
                    curDynamicObject.SetActive(true);
                }
                else
                {
                    if (PawnOwner != null && PawnOwner.SpawnedOrAnyParentSpawned)
                    {
                        if (PawnOwner.kindDef == null)
                        {
                            Log.Warning("NoneKindDef");
                        }
                        else
                        {
                            Log.Warning(PawnOwner.kindDef.defName);
                            CreateDynamicObject();
                        }
                        check = false;
                    }
                }
            }
            
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            if (curDynamicObject != null)
            {
                GameObject.Destroy(curDynamicObject);
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (curDynamicObject != null)
            {
                GameObject.Destroy(curDynamicObject);
            }
        }
    }
}
