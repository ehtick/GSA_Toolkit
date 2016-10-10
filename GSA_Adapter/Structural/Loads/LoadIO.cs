﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interop.gsa_8_7;
using BHG = BHoM.Geometry;
using BHE = BHoM.Structural.Elements;
using BHL = BHoM.Structural.Loads;
using BHB = BHoM.Base;
using GSA_Adapter.Utility;

namespace GSA_Adapter.Structural.Loads
{
    public class LoadIO
    {
        static public bool AddLoads(ComAuto gsa, List<BHL.ILoad> loads)
        {
            double[] unitFactors = Utils.GetUnitFactors(gsa);

            foreach (BHL.ILoad load in loads)
            {
                switch (load.LoadType)
                {
                    case BHL.LoadType.Selfweight:
                        AddGravityLoad(gsa, (BHL.GravityLoad)load);
                        break;

                    case BHL.LoadType.PointForce:
                        NodeLodeIO.AddNodalLoad(gsa, (BHL.PointForce)load, unitFactors[(int)GsaEnums.UnitType.FORCE]);
                        break;

                    case BHL.LoadType.PointDisplacement:
                        NodeLodeIO.AddNodalLoad(gsa, (BHL.PointDisplacement)load, unitFactors[(int)GsaEnums.UnitType.DISP]);
                        break;

                    case BHL.LoadType.Pressure:
                        BarLoadIO.AddPreStressLoad(gsa, (BHL.BarPrestressLoad)load);
                        break;

                    case BHL.LoadType.AreaUniformLoad:
                        AreaLoadIO.AddFaceLoad(gsa, load);
                        break;

                    case BHL.LoadType.PointVelocity:
                        NodeLodeIO.AddNodalLoad(gsa, (BHL.PointVelocity)load, 0/*TODO: IImplement velocityfactor if needed*/);
                        break;
                    case BHL.LoadType.PointAcceleration:
                        NodeLodeIO.AddNodalLoad(gsa, (BHL.PointAcceleration)load, unitFactors[(int)GsaEnums.UnitType.ACCEL]);
                        break;
                    case BHL.LoadType.BarPointLoad:
                    case BHL.LoadType.BarUniformLoad:
                    case BHL.LoadType.BarVaryingLoad:
                        BarLoadIO.AddBarLoad(gsa, (BHL.Load<BHE.Bar>)load, unitFactors[(int)GsaEnums.UnitType.LENGTH], unitFactors[(int)GsaEnums.UnitType.FORCE]);
                        break;
                    case BHL.LoadType.BarTemperature:
                        BarLoadIO.AddThermalLoad(gsa, (BHL.BarTemperatureLoad)load);
                        break;
                    case BHL.LoadType.PointMass:
                    case BHL.LoadType.AreaVaryingLoad:
                    case BHL.LoadType.AreaTemperature:
                    case BHL.LoadType.Geometrical:
                    default:
                        LoadNotImplementedWarning(load.LoadType.ToString());              
                        break;
                }


            }

            gsa.UpdateViews();
            return true;

        }

        public static void LoadNotImplementedWarning(string loadType)
        {
            Utils.SendErrorMessage("Load of type " + loadType + " is not implemented yet.");
        }

        public static void AddVectorDataToStringSingle(string startStr, BHG.Vector vec, ref List<string> strings, double factor, bool translational)
        {

            foreach (string str in GetForceVectorsStrings(vec,factor,translational))
            {
                strings.Add(startStr + "," + str);
            }

            //if (vec != null)
            //{
            //    string[] dir = LoadIO.Directions(translational);

            //    if (vec.X != 0)
            //        strings.Add(startStr + "," + dir[0] + "," + (factor * vec.X).ToString());
            //    if (vec.Y != 0)
            //        strings.Add(startStr + "," + dir[1] + "," + (factor * vec.Y).ToString());
            //    if (vec.Z != 0)
            //        strings.Add(startStr + "," + dir[2] + "," + (factor * vec.Z).ToString());
            //}
        }

        public static List<string> GetForceVectorsStrings(BHG.Vector vec, double factor, bool translational)
        {
            List<string> strings = new List<string>();

            if (vec != null)
            {
                string[] dir = LoadIO.Directions(translational);

                if (vec.X != 0)
                    strings.Add(dir[0] + "," + (factor * vec.X).ToString());
                if (vec.Y != 0)
                    strings.Add(dir[1] + "," + (factor * vec.Y).ToString());
                if (vec.Z != 0)
                    strings.Add(dir[2] + "," + (factor * vec.Z).ToString());
            }

            return strings;
        }

        public static string[] Directions(bool translations)
        {
            if (translations)
                return new string[] { "X", "Y", "Z" };
            else
                return new string[] { "XX", "YY", "ZZ" };
        }


        static public bool AddGravityLoad(ComAuto gsa, BHL.GravityLoad load)
        {
            string command = "LOAD_GRAVITY.2";
            string name = load.Name;
            string list = CreateIdListOrGroupName(gsa, load.Objects);

            string caseNo = load.Loadcase.Number.ToString();
            //if (!LoadcaseIO.GetOrCreateLoadCaseId(gsa, load.Loadcase, out caseNo)) { return false; }

            string x = load.GravityDriection.X.ToString();
            string y = load.GravityDriection.Y.ToString();
            string z = load.GravityDriection.Z.ToString();
            string str;

            str = command + ",," + list + "," + caseNo + "," + x + "," + y + "," + z;

            dynamic commandResult = gsa.GwaCommand(str);

            if (1 == (int)commandResult) return true;
            else
            {
                Utils.SendErrorMessage("Application of command " + command + " error. Invalid arguments?");
                return false;
            }
        }

        

        static public double GetUnitFactor(ComAuto GSA, GsaEnums.UnitType unitType)
        {
            string iUnitFactor = GSA.GwaCommand("GET, UNIT_DATA, " + unitType.ToString());

            string[] unitArray = iUnitFactor.Split(',');

            double unitFactor = Convert.ToDouble(unitArray[unitArray.Length - 1].Trim());

            return unitFactor;
        }

        public static string CreateIdListOrGroupName<T>(ComAuto gsa, BHB.Group<T> group) where T : BHB.BHoMObject
        {
            if (!string.IsNullOrWhiteSpace(group.Name))
                return "\"" + group.Name + "\"";

            List<string> ids;

            //if (group is BHB.Group<BHE.Node>)
            //    Elements.NodeIO.GetOrCreateNodes(gsa, group as List<BHE.Node>, out ids);
            //else
            //{
            List<T> idItems;

            bool isMesh = group is BHB.Group<BHE.FEMesh>;

            if (isMesh)
            {
                idItems = new List<T>();
                foreach (BHE.FEMesh mesh in group as BHB.Group<BHE.FEMesh>)
                {
                    foreach (BHE.FEFace face in mesh.Faces)
                    {
                        if (face.CustomData.ContainsKey(Utils.ID))
                            idItems.Add(face as T);
                        else
                            return null;
                    }
                }
            }
            else
            {

                List<T> nonIdItems = group.Where(x => !x.CustomData.ContainsKey(Utils.ID)).ToList();

                if (nonIdItems.Count > 0)
                    return null;

                idItems = group.Where(x => x.CustomData.ContainsKey(Utils.ID)).ToList();
            }

            ids = idItems.Select(x => x.CustomData[Utils.ID].ToString()).ToList();

            IEnumerable<int> intIds = ids.Select(x => int.Parse(x));

            return Utils.GeterateIdString(intIds);
        }

        //public static string CreateBarIDList(List<BHE.Bar> bars)
        //{
        //    string str = "";
        //    foreach (BHE.Bar bar in bars)
        //    {
        //        str = str + " " + bar.Name;
        //    }
        //    return str;
        //}

        //public static string CreatePanelIDList(List<BHE.Panel> panels)
        //{
        //    string str = "";
        //    foreach (BHE.Panel panel in panels)
        //    {
        //        str = str + " " + panel.Name;
        //    }
        //    return str;
        //}
    }
}
