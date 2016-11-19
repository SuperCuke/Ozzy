﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if NUGET
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

#if NUGET
namespace EventSourceProxy.NuGet
#else
namespace EventSourceProxy
#endif
{
	/// <summary>
	/// Helps emit attribute data to IL.
	/// </summary>
	static class EventSourceAttributeHelper
	{
		#region EventSourceAttribute Members
		/// <summary>
		/// The constructor for EventSourceAttribute.
		/// </summary>
		private static ConstructorInfo _eventSourceAttributeConstructor = typeof(EventSourceAttribute).GetTypeInfo().GetConstructor(Type.EmptyTypes);

		/// <summary>
		/// The array of properties used to serialize the custom attribute values.
		/// </summary>
		private static PropertyInfo[] _eventSourceAttributePropertyInfo = new PropertyInfo[]
		{
			typeof(EventSourceAttribute).GetTypeInfo().GetProperty("Name"),
			typeof(EventSourceAttribute).GetTypeInfo().GetProperty("Guid"),
			typeof(EventSourceAttribute).GetTypeInfo().GetProperty("LocalizationResources"),
		};

		/// <summary>
		/// Manages a list of the types that have been implemented so far.
		/// </summary>
		private static List<Type> _typesImplemented = new List<Type>();

		/// <summary>
		/// An empty parameter list.
		/// </summary>
		private static object[] _emptyParameters = new object[0];
		#endregion

		#region Helper Methods
		/// <summary>
		/// Copies the EventSourceAttribute from the interfaceType to a CustomAttributeBuilder.
		/// </summary>
		/// <param name="type">The interfaceType to copy.</param>
		/// <returns>A CustomAttributeBuilder that can be assigned to a type.</returns>
		internal static CustomAttributeBuilder GetEventSourceAttributeBuilder(Type type)
		{
			var attribute = type.GetTypeInfo().GetCustomAttribute<EventSourceAttribute>() ?? new EventSourceAttribute();
			var implementation = EventSourceImplementationAttribute.GetAttributeFor(type);

			// by default, we will use a null guid, which will tell EventSource to generate the guid from the name
			// but if we have already generated this type, we will have to generate a new one
			string guid = implementation.Guid ?? attribute.Guid ?? null;
			if (guid == null)
			{
				lock (_typesImplemented)
				{
					if (_typesImplemented.Contains(type))
						guid = Guid.NewGuid().ToString();
					else
						_typesImplemented.Add(type);
				}
			}

			var propertyValues = new object[]
			{
				implementation.Name ?? attribute.Name ?? (type.GetTypeInfo().IsGenericType ? type.FullName : type.Name),
				guid,
				implementation.LocalizationResources ?? attribute.LocalizationResources ?? null
			};

			CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(
				_eventSourceAttributeConstructor,
				_emptyParameters,
				_eventSourceAttributePropertyInfo,
				propertyValues);

			return attributeBuilder;
		}
		#endregion
	}
}
