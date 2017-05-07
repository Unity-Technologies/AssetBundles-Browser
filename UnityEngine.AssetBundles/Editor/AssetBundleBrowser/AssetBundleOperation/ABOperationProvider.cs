using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using System.Reflection;

namespace UnityEngine.AssetBundles.AssetBundleOperation
{
	public interface ABOperationProvider
    {
		int GetABOperationCount ();
		ABOperation CreateOperation(int index);
    }

	/**
	 * Used to declare the class is used as an ABOperation. 
	 * Classes with CustomABOperation attribute must implement ABOperation interface.
	 */ 
	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomABOperationProvider : Attribute {

		private string m_name;
		private int m_orderPriority;

		public static readonly int kDEFAULT_PRIORITY = 1000;

		public string Name {
			get {
				return m_name;
			}
		}

		public int OrderPriority {
			get {
				return m_orderPriority;
			}
		}

		public CustomABOperationProvider (string name) {
			m_name = name;
			m_orderPriority = kDEFAULT_PRIORITY;
		}

		public CustomABOperationProvider (string name, int orderPriority) {
			m_name = name;
			m_orderPriority = orderPriority;
		}
	}

	public struct CustomABOperationProviderInfo : IComparable {
		public CustomABOperationProvider provider;
		public Type type;

		public CustomABOperationProviderInfo(Type t, CustomABOperationProvider p) {
			provider = p;
			type = t;
		}

		public ABOperationProvider CreateInstance() {
			string typeName = type.FullName;

			object o = Assembly.GetExecutingAssembly().CreateInstance(typeName);
			return (ABOperationProvider) o;
		}

		public int CompareTo(object obj) {
			if (obj == null) {
				return 1;
			}

			CustomABOperationProviderInfo rhs = (CustomABOperationProviderInfo)obj;
			return provider.OrderPriority - rhs.provider.OrderPriority;
		}
	}

	public class ABOperationProviderUtility {

		private static List<CustomABOperationProviderInfo> s_customNodes;

		public static List<CustomABOperationProviderInfo> CustomABOperationProviderTypes {
			get {
				if(s_customNodes == null) {
					s_customNodes = BuildCustomABOperationProviderList();
				}
				return s_customNodes;
			}
		}

		private static List<CustomABOperationProviderInfo> BuildCustomABOperationProviderList() {
			var list = new List<CustomABOperationProviderInfo>();

			var nodes = Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.Where(t => t != typeof(ABOperationProvider))
				.Where(t => typeof(ABOperationProvider).IsAssignableFrom(t));

			foreach (var type in nodes) {
				CustomABOperationProvider attr = 
					type.GetCustomAttributes(typeof(CustomABOperationProvider), false).FirstOrDefault() as CustomABOperationProvider;

				if (attr != null) {
					list.Add(new CustomABOperationProviderInfo(type, attr));
				}
			}

			list.Sort();

			return list;
		}

		public static bool HasValidCustomABOperationProviderAttribute(Type t) {
			CustomABOperationProvider attr = 
				t.GetCustomAttributes(typeof(CustomABOperationProvider), false).FirstOrDefault() as CustomABOperationProvider;
			return attr != null && !string.IsNullOrEmpty(attr.Name);
		}

		public static string GetABOperationProviderName(ABOperationProvider provider) {
			CustomABOperationProvider attr = 
				provider.GetType().GetCustomAttributes(typeof(CustomABOperationProvider), false).FirstOrDefault() as CustomABOperationProvider;
			if(attr != null) {
				return attr.Name;
			}
			return string.Empty;
		}

		public static string GetABOperationProviderName(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomABOperationProvider attr = 
					Type.GetType(className).GetCustomAttributes(typeof(CustomABOperationProvider), false).FirstOrDefault() as CustomABOperationProvider;
				if(attr != null) {
					return attr.Name;
				}
			}
			return string.Empty;
		}

		public static int GetABOperationProviderOrderPriority(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomABOperationProvider attr = 
					Type.GetType(className).GetCustomAttributes(typeof(CustomABOperationProvider), false).FirstOrDefault() as CustomABOperationProvider;
				if(attr != null) {
					return attr.OrderPriority;
				}
			}
			return CustomABOperationProvider.kDEFAULT_PRIORITY;
		}

		public static ABOperationProvider CreateABOperationProviderInstance(string className) {
			if(className != null) {
				return (ABOperationProvider) Assembly.GetExecutingAssembly().CreateInstance(className);
			}
			return null;
		}
	}
}
