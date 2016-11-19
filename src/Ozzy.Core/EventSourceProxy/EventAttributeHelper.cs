﻿using System;
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
	static class EventAttributeHelper
	{
		#region EventAttribute Members
		/// <summary>
		/// The constructor for EventAttribute.
		/// </summary>
		private static ConstructorInfo _eventAttributeConstructor = typeof(EventAttribute).GetTypeInfo().GetConstructor(new[] { typeof(int) });

		/// <summary>
		/// The array of properties used to serialize the custom attribute values.
		/// </summary>
		private static PropertyInfo[] _eventAttributePropertyInfo = new PropertyInfo[]
		{
			typeof(EventAttribute).GetTypeInfo().GetProperty("Keywords"),
			typeof(EventAttribute).GetTypeInfo().GetProperty("Level"),
			typeof(EventAttribute).GetTypeInfo().GetProperty("Message"),
			typeof(EventAttribute).GetTypeInfo().GetProperty("Opcode"),
			typeof(EventAttribute).GetTypeInfo().GetProperty("Task"),
			typeof(EventAttribute).GetTypeInfo().GetProperty("Version"),
#if NUGET
			typeof(EventAttribute).GetProperty("Channel"),
#endif	
		};

		/// <summary>
		/// A set of empty parameters that can be sent to a method call.
		/// </summary>
		private static object[] _emptyParameters = new object[0];
		#endregion

		/// <summary>
		/// Converts an EventAttribute to a CustomAttributeBuilder so it can be assigned to a method.
		/// </summary>
		/// <param name="attribute">The attribute to copy.</param>
		/// <returns>A CustomAttributeBuilder that can be assigned to a method.</returns>
		internal static CustomAttributeBuilder ConvertEventAttributeToAttributeBuilder(EventAttribute attribute)
		{
			var propertyValues = new object[]
			{
				attribute.Keywords,
				attribute.Level,
				attribute.Message,
				attribute.Opcode,
				attribute.Task,
				attribute.Version,
#if NUGET
				attribute.Channel
#endif
			};

			CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(
				_eventAttributeConstructor,
				new object[] { attribute.EventId },
				_eventAttributePropertyInfo,
				propertyValues);

			return attributeBuilder;
		}

		/// <summary>
		/// Creates an empty NonEventAttribute.
		/// </summary>
		/// <returns>A CustomAttributeBuilder that can be added to a method.</returns>
		internal static CustomAttributeBuilder CreateNonEventAttribute()
		{
			return new CustomAttributeBuilder(typeof(NonEventAttribute).GetTypeInfo().GetConstructor(Type.EmptyTypes), _emptyParameters);
		}
	}
}
