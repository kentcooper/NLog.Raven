using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;

namespace NLog.Raven
{
    public class NLogEntry : DynamicObject
    {
        [NonSerialized] private readonly Dictionary<string, object> _fields = new Dictionary<string, object>();

        public object this[string index]
        {
            get => _fields[index];
            set => _fields[index] = value;
        }

        /// <summary>
        ///     Provides the implementation for operations that get member values. Classes derived from the
        ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
        ///     operations such as getting a value for a property.
        /// </summary>
        /// <param name="binder">
        ///     Provides information about the object that called the dynamic operation. The binder.Name property
        ///     provides the name of the member on which the dynamic operation is performed. For example, for the
        ///     Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived
        ///     from the <see cref="T:System.Dynamic.DynamicObject" /> class, binder.Name returns "SampleProperty". The
        ///     binder.IgnoreCase property specifies whether the member name is case-sensitive.
        /// </param>
        /// <param name="result">
        ///     The result of the get operation. For example, if the method is called for a property, you can
        ///     assign the property value to <paramref name="result" />.
        /// </param>
        /// <returns>
        ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
        ///     the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
             Justification = "Since this is a 'Try' method we do not want it to throw an exception.")]
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return _fields.TryGetValue(binder.Name, out result);
        }

        /// <summary>
        ///     Provides the implementation for operations that set member values. Classes derived from the
        ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
        ///     operations such as setting a value for a property.
        /// </summary>
        /// <param name="binder">
        ///     Provides information about the object that called the dynamic operation. The binder.Name property
        ///     provides the name of the member to which the value is being assigned. For example, for the statement
        ///     sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the
        ///     <see cref="T:System.Dynamic.DynamicObject" /> class, binder.Name returns "SampleProperty". The binder.IgnoreCase
        ///     property specifies whether the member name is case-sensitive.
        /// </param>
        /// <param name="value">
        ///     The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where
        ///     sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject" /> class, the
        ///     <paramref name="value" /> is "Test".
        /// </param>
        /// <returns>
        ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
        ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        /// <exception cref="System.ArgumentException">'binder' cannot be a null value;binder</exception>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
             Justification = "Since this is a 'Try' method we do not want it to throw an exception.")]
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (binder == null)
            {
                throw new ArgumentException("'binder' cannot be a null value", nameof(binder));
            }
            try
            {
                if (_fields.ContainsKey(binder.Name))
                {
                    _fields[binder.Name] = value;
                }
                else
                {
                    _fields.Add(binder.Name, value);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Provides the implementation for operations that set a value by index. Classes derived from the
        ///     <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify dynamic behavior for
        ///     operations that access objects by a specified index.
        /// </summary>
        /// <param name="binder">Provides information about the operation.</param>
        /// <param name="indexes">
        ///     The indexes that are used in the operation. For example, for the sampleObject[3] = 10 operation
        ///     in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the
        ///     <see cref="T:System.Dynamic.DynamicObject" /> class, <paramref name="indexes[0]" /> is equal to 3.
        /// </param>
        /// <param name="value">
        ///     The value to set to the object that has the specified index. For example, for the sampleObject[3] =
        ///     10 operation in C# (sampleObject(3) = 10 in Visual Basic), where sampleObject is derived from the
        ///     <see cref="T:System.Dynamic.DynamicObject" /> class, <paramref name="value" /> is equal to 10.
        /// </param>
        /// <returns>
        ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
        ///     the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.
        /// </returns>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            var key = (string)indexes[0];
            _fields[key] = value;
            return true;
        }

        /// <summary>
        ///     Provides the implementation for operations that get a value by index.
        /// </summary>
        /// <param name="binder">Provides information about the operation.</param>
        /// <param name="indexes">
        ///     The indexes that are used in the operation. For example, for the sampleObject[3] operation in C#
        ///     (sampleObject(3) in Visual Basic), where sampleObject is derived from the DynamicObject class,
        ///     <paramref name="indexes[0]" /> is equal to 3.
        /// </param>
        /// <param name="result">The result of the index operation.</param>
        /// <returns>
        ///     true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of
        ///     the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            return _fields.TryGetValue((string)indexes[0], out result);
        }

        /// <summary>
        ///     Provides the implementation for operations that get a value by key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> true if the operation is successful; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(string key, out object value)
        {
            var response = _fields.TryGetValue(key, out value);
            // ReSharper disable once InvertIf
            if (!response)
            {
                response = _fields.Any(a => string.Equals(a.Key, key, StringComparison.InvariantCultureIgnoreCase));
                if (response)
                {
                    value =
                        _fields.First(a => string.Equals(a.Key, key, StringComparison.InvariantCultureIgnoreCase))
                            .Value;
                }
            }

            return response;
        }

        public object GetValue(string key)
        {
            _fields.TryGetValue(key, out object value);

            return value;
        }

        /// <summary>
        ///     Returns a list of the Dynamic Members of this type. However in order to properly serialize to RavenDB we also add
        ///     the static members.
        /// </summary>
        /// <returns>A sequence that contains dynamic member names.</returns>
        /// <remarks>This method is required to be overridden in order to use a dynamic object in RavenDb</remarks>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            var members = new List<string>();
            members.AddRange(_fields.Keys);
            return members;
        }
    }
}