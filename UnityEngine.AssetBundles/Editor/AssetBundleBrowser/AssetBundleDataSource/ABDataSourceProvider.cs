using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine.AssetBundles.AssetBundleDataSource
{
    public class ABDataSourceProviderUtility {

        private static List<Type> s_customNodes;

        public static List<Type> CustomABDataSourceTypes {
            get {
                if(s_customNodes == null) {
                    s_customNodes = BuildCustomABDataSourceList();
                }
                return s_customNodes;
            }
        }

        private static List<Type> BuildCustomABDataSourceList() {
            var list = new List<Type>(Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => t != typeof(ABDataSource))
                .Where(t => typeof(ABDataSource).IsAssignableFrom(t)) );


            var properList = new List<Type>();
            properList.Add(null); //empty spot for "default" 
            for(int count = 0; count < list.Count; count++)
            {
                if(list[count].Name == "AssetDatabaseABDataSource")
                    properList[0] = list[count];
                else
                    properList.Add(list[count]);
            }

            return properList;
        }
    }
}
