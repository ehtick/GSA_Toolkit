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

using BH.oM.Structure.Elements;
using BH.oM.Structure.Loads;
using BH.oM.Geometry;
using System.Collections.Generic;


namespace BH.Adapter.GSA
{
    public static partial class Convert
    {
        /***************************************************/
        /**** Public  Methods                           ****/
        /***************************************************/

        public static List<string> ToGsaString(this IElementLoad<Node> nodeLoad, double[] unitFactors)
        {
            string command;
            List<string> forceStrings = new List<string>();
            double factor;

            Vector[] force = nodeLoad.ITranslationVector();
            command = nodeLoad.IForceTypeString();
            Vector[] moment = nodeLoad.IRotationVector();
            factor = nodeLoad.IFactor(unitFactors);

            string name = nodeLoad.Name;
            string str;
            string appliedTo = nodeLoad.CreateIdListOrGroupName();
            string caseNo = nodeLoad.Loadcase.Number.ToString();
            string axis = IsGlobal(nodeLoad);
            string[] pos = { ("," + nodeLoad.ILoadPosition()[0]), ("," + nodeLoad.ILoadPosition()[1]) };

            str = command + "," + name + "," + appliedTo + "," + caseNo + "," + axis;

            VectorDataToString(str, force, ref forceStrings, factor, true, pos);
            VectorDataToString(str, moment, ref forceStrings, factor, false, pos);

            return forceStrings;
        }

        /***************************************************/

    }
}


