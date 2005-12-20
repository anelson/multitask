using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace MultiTask
{
    /// <summary>Class which implements a reflection-based deep copy, which deep-copies an
    ///     entire object graph, optionally skipping specific types from the copy.</summary>
    class DeepCopier
    {
		ArrayList _dontCopyTypes;
		Hashtable _copiedObjects;

		public DeepCopier(Type[] dontCopyTypes) {
			_dontCopyTypes = new ArrayList(dontCopyTypes);
            //This class is always on the dont-copy list
            _dontCopyTypes.Add(typeof(DeepCopier));
		}
		
        public Object DeepCopyObject(Object obj)
        {
			_copiedObjects = new Hashtable();

			return InternalDeepCopyObject(obj);
        }

		private Object InternalDeepCopyObject(Object obj) {
			//If the type of obj is in the dont-copy list, return it straightwawy
			if (_dontCopyTypes.Contains(obj.GetType())) {
				return obj;
			}

			//Else, deep copy it, or pull it from the cache
			if (!_copiedObjects.Contains(obj)) {
                Object copy = CreateUninitializedObject(obj);

                _copiedObjects.Add(obj, copy);
				
				CopyFields(obj, copy);
			}

            return _copiedObjects[obj];
		}

		private Object CreateUninitializedObject(Object src) {
			Type type = src.GetType();
			if (type.IsArray) {
				//Can't use GetUninitializedObject on an array.  Create an empty array, and
				//fill it with uninitialized objects
				Type elementType = type.GetElementType();
				int rank = type.GetArrayRank();
				int[] dimensions = new int[rank];

				for (int dimension = 0; dimension < rank; dimension++) {
					dimensions[dimension] = ((Array)src).GetLength(dimension);
				}
				
				Array arr = Array.CreateInstance(type.GetElementType(), dimensions);

				if (arr.GetLength(0) > 0) {
					//Fill this array with uninitialized objects
					int[] indexes = new int[rank];

					do {
						arr.SetValue(FormatterServices.GetUninitializedObject(elementType),
									 indexes);
					} while (IncrementMultidimIndex(indexes, dimensions));
				}

                return arr;
			} else if (type == typeof(String)) {
				//Strings are a special case; you can't GetUninitializedObject on them, since they're
				//immutable.  So, return the source
				return src;
			} else {
				return FormatterServices.GetUninitializedObject(type);
			}
		}

		private bool IncrementMultidimIndex(int[] indexes, int[] dimensions) {
			if (indexes.Length != dimensions.Length) {
				throw new ArgumentException("Both arrays must have the same length", "dimensions");
			}

			int i = 0;

			while (i < indexes.Length && (++indexes[i]) >= dimensions[i]) {
				indexes[i] = 0;
				i++;
			}

			if (i >= indexes.Length) {
				return false;
			}

			return true;
		}

		private void CopyFields(Object src, Object dest) {
			if (src.GetType() != dest.GetType()) {
				throw new ArgumentException("Source and destination objects must be of the same type", "dest");
			}

			Type type = src.GetType();

			while (type != null) {
				//Copy all non-static fields, regardless of accessibility
                foreach (FieldInfo fi in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
					Object srcValue = fi.GetValue(src);
					if (srcValue != null) {
						fi.SetValue(dest, InternalDeepCopyObject(srcValue));
					}
				}

				type = type.BaseType;
			}
		}
    }
}
