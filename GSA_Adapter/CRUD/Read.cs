/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2022, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */


#if GSA_10_1
using Interop.Gsa_10_1;
#else
using Interop.gsa_8_7;
#endif
using BH.oM.Base;
using BH.oM.Structure.MaterialFragments;
using BH.oM.Structure.Elements;
using BH.oM.Structure.SectionProperties;
using BH.oM.Structure.SurfaceProperties;
using BH.oM.Structure.Constraints;
using BH.oM.Structure.Loads;
using BH.oM.Geometry;
using BH.oM.Structure.Results;
using BH.oM.Structure.Requests;
using BH.oM.Analytical.Results;
using System;
using BH.Engine.Adapter;
using BH.oM.Adapters.GSA;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BH.oM.Adapter;
using BH.Engine.Adapters.GSA;

namespace BH.Adapter.GSA
{
#if GSA_10_1
    public partial class GSA101Adapter
#else
    public partial class GSA87Adapter
#endif
    {
        /***************************************************/
        /**** Index Adapter Methods                     ****/
        /***************************************************/

        protected override IEnumerable<IBHoMObject> IRead(Type type, IList indices, ActionConfig actionConfig = null)
        {
            if (type == typeof(Node))
                return ReadNodes(indices as dynamic);
            else if (type == typeof(Bar))
                return ReadBars(indices as dynamic);
            else if (type == typeof(ISectionProperty) || type.GetInterfaces().Contains(typeof(ISectionProperty)))
                return ((List<ISectionProperty>)ReadSectionProperties(indices as dynamic)).Cast<BHoMObject>();
            else if (typeof(IMaterialFragment).IsAssignableFrom(type))
                return ReadMaterials(indices as dynamic);
            else if (type == typeof(LoadCombination))
                return ReadLoadCombinations(indices as dynamic);
            else if (type == typeof(ILoad) || type.GetInterfaces().Contains(typeof(ILoad)))
                return new List<ILoad>(); //TODO: Implement load extraction
            if (type == typeof(RigidLink))
                return ReadRigidLink(indices as dynamic);
            if (type == typeof(LinkConstraint))
                return ReadLinkConstraint(indices as dynamic);
            if (type == typeof(FEMesh))
                return ReadFEMesh(indices as dynamic);
            if (type == typeof(ISurfaceProperty))
                return ReadProperty2d(indices as dynamic);
            if (type == typeof(Loadcase))
                return ReadLoadCases(indices as dynamic);
            if (type.IsGenericType && type.Name == typeof(BHoMGroup<IBHoMObject>).Name)
                return new List<BHoMGroup<IBHoMObject>>();
            if (typeof(IResult).IsAssignableFrom(type))
            {
                Modules.Structure.ErrorMessages.ReadResultsError(type);
                return null;
            }

            return null;
        }

        /***************************************************/
        /**** Protected Methods                         ****/
        /***************************************************/

        public List<IMaterialFragment> ReadMaterials(List<string> ids = null)
        {
            return ReadMaterials(ids, false);
        }

        /***************************************************/

        public List<IMaterialFragment> ReadMaterials(List<string> ids = null, bool includeStandard = false)
        {
            List<IMaterialFragment> materials = ReadMaterialDictionary(ids, includeStandard).Select(x => x.Value).ToList();

            if (ids != null && ids.Count != 0)
                materials = materials.Where(x => ids.Contains(x.GSAId().ToString())).ToList();

            return materials;
        }

        public Dictionary<string, IMaterialFragment> ReadMaterialDictionary(List<string> ids = null, bool includeStandard = false)
        {
            //List<IMaterialFragment> materials = ReadMaterials(null, false);
            List<IMaterialFragment> materials = new List<IMaterialFragment>();
            List<string> keys = new List<string>();
            Dictionary<string, IMaterialFragment> materialDictionary = new Dictionary<string, IMaterialFragment>();

#if GSA_10_1
            string allProps = m_gsaCom.GwaCommand("GET_ALL, MAT_ANAL.1").ToString();
            allProps += "\n" + m_gsaCom.GwaCommand("GET_ALL, MAT_STEEL.3").ToString();
            allProps += "\n" + m_gsaCom.GwaCommand("GET_ALL, MAT_CONCRETE.17").ToString();
            allProps += "\n" + m_gsaCom.GwaCommand("GET_ALL, MAT_FRP.3").ToString();
            allProps += "\n" + m_gsaCom.GwaCommand("GET_ALL, MAT_ALUMINIUM.9").ToString();
            allProps += "\n" + m_gsaCom.GwaCommand("GET_ALL, MAT_TIMBER.10").ToString();
            allProps += "\n" + m_gsaCom.GwaCommand("GET_ALL, MAT_GLASS.10").ToString();
            allProps += "\n" + m_gsaCom.GwaCommand("GET_ALL, MAT_FABRIC.10").ToString();
            allProps += "\n" + m_gsaCom.GwaCommand("GET_ALL, MAT_REBAR.1").ToString();
#else
            string allProps = m_gsaCom.GwaCommand("GET_ALL, MAT").ToString();
#endif
            string[] matArr = string.IsNullOrWhiteSpace(allProps) ? new string[0] : allProps.Split('\n');
            matArr = matArr.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            if (ids == null)
                materials = matArr.Select(x => Convert.FromGsaMaterial(x)).Where(x => x != null).ToList();
            else
                materials = matArr.Where(x => ids.Contains(x.Split(',')[1])).Select(x => Convert.FromGsaMaterial(x)).Where(x => x != null).ToList();

            if (includeStandard)
                materials.AddRange(GetStandardGsaMaterials());

            for (int i = 0; i < materials.Count(); i++)
            {
#if GSA_10_1
                string key = matArr[i].Split(("_.").ToCharArray())[1] + ":" + materials[i].GSAId();
#else
                string key = GetAdapterId(materials[i]).ToString();
#endif
                if (!keys.Contains(key))
                {
                    keys.Add(key);
                    materialDictionary.Add(key, materials[i]);
                }
            }

            return materialDictionary;
        }

        /***************************************************/

        public List<Loadcase> ReadLoadCases(List<string> ids = null)
        {
            List<Loadcase> lCases = new List<Loadcase>();

            string allLoadCases = m_gsaCom.GwaCommand("GET_ALL, LOAD_TITLE.1").ToString();
            string[] lCaseArr = string.IsNullOrWhiteSpace(allLoadCases) ? new string[0] : allLoadCases.Split('\n');

            if (ids == null)
                lCases = lCaseArr.Select(x => Convert.FromGsaLoadcase(x)).ToList();
            else
                lCases = lCaseArr.Where(x => ids.Contains(x.Split(',')[1])).Select(x => Convert.FromGsaLoadcase(x)).ToList();

            return lCases;
        }

        /***************************************/

        public List<Bar> ReadBars(List<string> ids = null)
        {
            //int[] potentialBeamRefs = GenerateIndices(ids, typeof(Bar));

            //GsaElement[] gsaElements = new GsaElement[potentialBeamRefs.Length];
            //m_gsaCom.Elements(potentialBeamRefs, out gsaElements);

            string allNodes = m_gsaCom.GwaCommand("GET_ALL, EL.2").ToString();

            string[] barArr = string.IsNullOrWhiteSpace(allNodes) ? new string[0] : allNodes.Split('\n');

            List<ISectionProperty> secPropList = ReadSectionProperties();
            List<Node> nodeList = ReadNodes();

            Dictionary<string, ISectionProperty> secProps = secPropList.Where(x => x != null).ToDictionary(x => GetAdapterId<int>(x).ToString());
            Dictionary<string, Node> nodes = nodeList.ToDictionary(x => GetAdapterId<int>(x).ToString());

            return Convert.FromGsaBars(barArr, secProps, nodes, ids);
        }

        /***************************************/

        public List<LoadCombination> ReadLoadCombinations(List<string> ids = null)
        {

            List<LoadCombination> lComabinations = new List<LoadCombination>();
            int loadCasesCount = m_gsaCom.GwaCommand("HIGHEST, ANAL");
            List<string> analList = new List<string>();
            for (int i = 0; i < loadCasesCount; i++)
            {
                string anal = m_gsaCom.GwaCommand("GET, ANAL," + (i + 1)).ToString();
                if (!string.IsNullOrWhiteSpace(anal))
                    analList.Add(anal);
            }

            List<Loadcase> lCaseList = ReadLoadCases();
            Dictionary<string, Loadcase> lCases = lCaseList.ToDictionary(x => x.Number.ToString());

            if (ids == null)
                lComabinations = analList.Select(x => Convert.FromGsaAnalTask(x, lCases)).ToList();
            else
                lComabinations = analList.Where(x => ids.Contains(x.Split(',')[1])).Select(x => Convert.FromGsaAnalTask(x, lCases)).ToList();

            return lComabinations;
        }

        /***************************************/

        public List<Node> ReadNodes(List<string> ids = null)
        {
            //GsaNode[] gsaNodes;
            //m_gsaCom.Nodes(GenerateIndices(ids, typeof(Node)), out gsaNodes);

            //return gsaNodes.Select(x => Convert.FromGsaNode(x)).ToList();
            string allNodes = m_gsaCom.GwaCommand("GET_ALL, NODE").ToString();
            string[] nodeArr = string.IsNullOrWhiteSpace(allNodes) ? new string[0] : allNodes.Split('\n');

            Dictionary<int, Basis> axes = ReadAxes();
            Dictionary<int, double[]> dampProp = ReadDampProperty();
            Dictionary<int, double[]> springValues = ReadSpingValues();
            if (ids == null)
                return nodeArr.Select(x => Convert.FromGsaNode(x, dampProp, springValues, axes)).ToList();
            else
                return nodeArr.Where(x => ids.Contains(x.Split(',')[1])).Select(x => Convert.FromGsaNode(x, dampProp, springValues, axes)).ToList();
        }

        /***************************************/

        public Dictionary<int, double[]> ReadDampProperty()
        {
            Dictionary<int, double[]> dampPropertyDictionary = new Dictionary<int, double[]>();
#if GSA_10_1
            string allDamp = m_gsaCom.GwaCommand("GET_ALL, PROP_DAMP.2").ToString();
            string[] dampProp = string.IsNullOrWhiteSpace(allDamp) ? new string[0] : allDamp.Split('\n');

            foreach (string a in dampProp)
            {
                string[] arr = a.Split(',');
                double[] stiff = new List<double>() { Double.Parse(arr[5]), Double.Parse(arr[6]), Double.Parse(arr[7]), Double.Parse(arr[8]), Double.Parse(arr[9]), Double.Parse(arr[10]) }.ToArray();
                dampPropertyDictionary.Add(Int32.Parse(arr[1]), stiff);
            }

            return dampPropertyDictionary;
#else
            return null;
#endif
        }

        /***************************************/

        public Dictionary<int, double[]> ReadSpingValues()
        {
            Dictionary<int, double[]> dampPropertyDictionary = new Dictionary<int, double[]>();
#if GSA_10_1
            string allSpings = m_gsaCom.GwaCommand("GET_ALL, PROP_SPR.4").ToString();
            string[] springProp = string.IsNullOrWhiteSpace(allSpings) ? new string[0] : allSpings.Split('\n');

            foreach (string a in springProp)
            {
                string[] arr = a.Split(',');
                double[] stiff = new List<double>() { Double.Parse(arr[6]), Double.Parse(arr[8]), Double.Parse(arr[10]), Double.Parse(arr[12]), Double.Parse(arr[14]), Double.Parse(arr[16]) }.ToArray();
                dampPropertyDictionary.Add(Int32.Parse(arr[1]), stiff);
            }

            return dampPropertyDictionary;
#else
            return null;
#endif
        }

        /***************************************/

        private Dictionary<int, Basis> ReadAxes(List<string> ids = null)
        {
            string allAxes = m_gsaCom.GwaCommand("GET_ALL, AXIS").ToString();
            string[] axesArr = string.IsNullOrWhiteSpace(allAxes) ? new string[0] : allAxes.Split('\n');

            Dictionary<int, Basis> axes = new Dictionary<int, Basis>();

            foreach (string axisString in axesArr)
            {
                int id;
                Basis basis = Convert.FromGsaAxis(axisString, out id);
                if (basis != null)
                    axes[id] = basis;
            }

            return axes;
        }

        /***************************************/

        public List<ISectionProperty> ReadSectionProperties(List<string> ids = null)
        {

            Dictionary<string, IMaterialFragment> materials = ReadMaterialDictionary(null, false);

#if GSA_10_1
            string allProps = m_gsaCom.GwaCommand("GET_ALL, SECTION.7").ToString();
#else
            string allProps = m_gsaCom.GwaCommand("GET_ALL, PROP_SEC").ToString();
#endif
            string[] proArr = string.IsNullOrWhiteSpace(allProps) ? new string[0] : allProps.Split('\n');

            if (ids == null)
                return proArr.Select(x => Convert.FromGsaSectionProperty(x, materials)).ToList();
            else
                return proArr.Where(x => ids.Contains(x.Split(',')[1])).Select(x => Convert.FromGsaSectionProperty(x, materials)).ToList();
        }

        /***************************************/

        public List<ISurfaceProperty> ReadProperty2d(List<string> ids = null)
        {
            Dictionary<string, IMaterialFragment> materials = ReadMaterialDictionary(null, false);
#if GSA_10_1
            string allProps = m_gsaCom.GwaCommand("GET_ALL, PROP_2D.7").ToString();
#else
            string allProps = m_gsaCom.GwaCommand("GET_ALL, PROP_2D").ToString();
#endif
            string[] proArr = string.IsNullOrWhiteSpace(allProps) ? new string[0] : allProps.Split('\n');

            if (ids == null)
                return proArr.Select(x => Convert.FromGsaSurfaceProperty(x, materials)).ToList();
            else
                return proArr.Where(x => ids.Contains(x.Split(',')[1])).Select(x => Convert.FromGsaSurfaceProperty(x, materials)).ToList();
        }

        /***************************************/

        public List<FEMesh> ReadFEMesh(List<string> ids = null)
        {
            int[] potentialMeshRefs = GenerateIndices(ids, typeof(FEMesh));

            GsaElement[] gsaElements = new GsaElement[potentialMeshRefs.Length];
            m_gsaCom.Elements(potentialMeshRefs, out gsaElements);

            List<ISurfaceProperty> secPropList = ReadProperty2d();
            List<Node> nodeList = ReadNodes();

            Dictionary<string, ISurfaceProperty> props = secPropList.Where(x => x != null).ToDictionary(x => GetAdapterId<int>(x).ToString());
            Dictionary<string, Node> nodes = nodeList.ToDictionary(x => GetAdapterId<int>(x).ToString());

            return Convert.FromGsaFEMesh(gsaElements, props, nodes);
        }

        /***************************************/

        public List<LinkConstraint> ReadLinkConstraint(List<string> ids = null)
        {
            string allProps = m_gsaCom.GwaCommand("GET_ALL, PROP_LINK").ToString();
            string[] proArr = string.IsNullOrWhiteSpace(allProps) ? new string[0] : allProps.Split('\n');

            if (ids == null)
                return proArr.Select(x => Convert.FromGsaLinkConstraint(x)).ToList();
            else
                return proArr.Where(x => ids.Contains(x.Split(',')[1])).Select(x => Convert.FromGsaLinkConstraint(x)).ToList();
        }

        /***************************************/

        public List<RigidLink> ReadRigidLink(List<string> ids = null)
        {
            List<LinkConstraint> constraintList = ReadLinkConstraint(null);
            List<Node> nodeList = ReadNodes();

            Dictionary<string, LinkConstraint> constraints = constraintList.ToDictionary(x => GetAdapterId<int>(x).ToString());
            Dictionary<string, Node> nodes = nodeList.ToDictionary(x => GetAdapterId<int>(x).ToString());

            int[] potentialBeamRefs = GenerateIndices(ids, typeof(RigidLink));
            GsaElement[] gsaElements = new GsaElement[potentialBeamRefs.Length];
            m_gsaCom.Elements(potentialBeamRefs, out gsaElements);

            return Convert.FromGsaRigidLinks(gsaElements, constraints, nodes);

            //if (ids == null)
            //    return proArr.Select(x => Convert.FromGsaSectionProperty(x, materials)).ToList();
            //else
            //    return proArr.Where(x => ids.Contains(x.Split(',')[1])).Select(x => Convert.FromGsaSectionProperty(x, materials)).ToList();
        }


        /***************************************************/
        /**** Private  Methods                          ****/
        /***************************************************/

        private int[] GenerateIndices(List<string> ids, Type elementType)
        {
            if (ids == null)
            {
                int maxIndex = m_gsaCom.GwaCommand("HIGHEST, " + elementType.ToGsaString());
                maxIndex = maxIndex > 0 ? maxIndex : 1;
                int[] potentialBeamRefs = new int[maxIndex];
                for (int i = 0; i < maxIndex; i++)
                    potentialBeamRefs[i] = i + 1;

                return potentialBeamRefs;
            }

            return ids.Select(x => int.Parse(x)).ToArray();
        }

        /***************************************/

        private List<IMaterialFragment> GetStandardGsaMaterials()
        {
            List<IMaterialFragment> materials = new List<IMaterialFragment>();
            materials.Add(new Steel() { Name = "STEEL" });
            materials.Add(new Concrete() { Name = "CONC_SHORT" });
            materials.Add(new Concrete() { Name = "CONC_LONG" });
            materials.Add(new Aluminium() { Name = "ALUMINIUM" });

            foreach (IMaterialFragment material in materials)
            {
                SetAdapterId(material, material.Name);
            }

            return materials;
        }

        /***************************************************/
    }
}


