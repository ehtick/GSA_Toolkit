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

using BH.oM.Adapter;
using System.Collections.Generic;
using BH.oM.Base;
using BH.oM.Structure.Elements;
using BH.Engine.Adapter;
using BH.oM.Adapters.GSA;
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
        /**** Adapter Methods                           ****/
        /***************************************************/

        protected override bool IUpdate<T>(IEnumerable<T> objects, ActionConfig actionConfig = null)
        {
            return Update(objects as dynamic, actionConfig);
        }

        /***************************************************/

        private bool Update(IEnumerable<IBHoMObject> objects, ActionConfig actionConfig = null)
        {
            return ICreate(objects, actionConfig);
        }

        /***************************************************/

        private bool Update(IEnumerable<FEMesh> objects, ActionConfig actionConfig = null)
        {
            bool success = true;
            foreach (FEMesh mesh in objects)
            {
                if (mesh == null || GetAdapterId(mesh) == null || mesh.Faces.Count != 1)
                {
                    Engine.Base.Compute.RecordError("Can only update meshes with exactly one face and with a set adapter id");
                    success = false;
                    continue;
                }

                success &= ComCall(Convert.ToGsaString(mesh, GetAdapterId<int>(mesh), 0));
            }

            return success;
        }

        /***************************************************/
    }
}


