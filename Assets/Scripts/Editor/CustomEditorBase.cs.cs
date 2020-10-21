using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using System.Reflection;
using Assets.Scripts.Editor;
using System;
using System.Linq;

[CustomEditor(typeof(UnityEngine.Object), true, isFallback = true)]
[CanEditMultipleObjects]
public class CustomEditorBase : Editor
{
    private Dictionary<string, ReorderableListProperty> reorderableLists;

    protected virtual void OnEnable()
    {
        this.reorderableLists = new Dictionary<string, ReorderableListProperty>(10);
    }

    ~CustomEditorBase()
    {
        this.reorderableLists.Clear();
        this.reorderableLists = null;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Custom Editor", EditorStyles.centeredGreyMiniLabel);
        var cachedGuiColor = GUI.color;
        this.serializedObject.Update();
        var property = this.serializedObject.GetIterator();
        bool next = property.NextVisible(true);
        if (next)
        {
            do
            {
                GUI.color = cachedGuiColor;
                this.HandleProperty(property);
            } while (property.NextVisible(false));
        }
        this.serializedObject.ApplyModifiedProperties();
    }

    protected void HandleProperty(SerializedProperty property)
    {
        //Debug.LogFormat("name: {0}, displayName: {1}, type: {2}, propertyType: {3}, path: {4}", property.name, property.displayName, property.type, property.propertyType, property.propertyPath);
        bool isdefaultScriptProperty = property.name.Equals("m_Script") && property.type.Equals("PPtr<MonoScript>") && property.propertyType == SerializedPropertyType.ObjectReference && property.propertyPath.Equals("m_Script");
        bool cachedGUIEnabled = GUI.enabled;
        if (isdefaultScriptProperty)
            GUI.enabled = false;
        //var attr = this.GetPropertyAttributes(property);
        if (property.isArray && property.propertyType != SerializedPropertyType.String)
            this.HandleArray(property);
        else
            EditorGUILayout.PropertyField(property, property.isExpanded);
        if (isdefaultScriptProperty)
            GUI.enabled = cachedGUIEnabled;
    }

    protected void HandleArray(SerializedProperty property)
    {
        var listData = this.GetReorderableList(property);
        listData.IsExpanded.target = property.isExpanded;
        if (!listData.IsExpanded.value && !listData.IsExpanded.isAnimating || !listData.IsExpanded.value && listData.IsExpanded.isAnimating)
        {
            EditorGUILayout.BeginHorizontal();
            property.isExpanded = EditorGUILayout.ToggleLeft(string.Format("{0}[]", property.displayName), property.isExpanded, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(string.Format("size: {0}", property.arraySize));
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (EditorGUILayout.BeginFadeGroup(listData.IsExpanded.faded))
                listData.List.DoLayoutList();
            EditorGUILayout.EndFadeGroup();
        }
    }

    protected object[] GetPropertyAttributes(SerializedProperty property)
    {
        return this.GetPropertyAttributes<PropertyAttribute>(property);
    }

    protected object[] GetPropertyAttributes<T>(SerializedProperty property) where T : System.Attribute
    {
        var bindingFlags = System.Reflection.BindingFlags.GetField
            | System.Reflection.BindingFlags.GetProperty
            | System.Reflection.BindingFlags.IgnoreCase
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Public;
        if (property.serializedObject.targetObject == null)
            return null;
        var targetType = property.serializedObject.targetObject.GetType();
        var field = targetType.GetField(property.name, bindingFlags);
        return field != null ? field.GetCustomAttributes(typeof(T), true) : null;
    }

    private ReorderableListProperty GetReorderableList(SerializedProperty property)
    {
        if (this.reorderableLists.TryGetValue(property.name, out var ret))
        {
            ret.Property = property;
            return ret;
        }
        ret = new ReorderableListProperty(property);
        this.reorderableLists.Add(property.name, ret);
        return ret;
    }

    #region Inner-class ReorderableListProperty
    private class ReorderableListProperty
    {
        public AnimBool IsExpanded { get; private set; }

        /// <summary>
        /// ref http://va.lent.in/unity-make-your-lists-functional-with-reorderablelist/
        /// </summary>
        public ReorderableList List { get; private set; }

        private SerializedProperty _property;
        public SerializedProperty Property
        {
            get => this._property;
            set
            {
                this._property = value;
                this.List.serializedProperty = this._property;
            }
        }

        public ReorderableListProperty(SerializedProperty property)
        {
            this.IsExpanded = new AnimBool(property.isExpanded)
            {
                speed = 1f
            };
            this._property = property;
            this.CreateList();
        }

        ~ReorderableListProperty()
        {
            this._property = null;
            this.List = null;
        }

        static void CopyValues(object source, object dest)
        {
            var sourceProps = source.GetType().GetProperties()
                .Where(x => x.CanRead)
                ;
            var propPairs = dest.GetType().GetProperties()
                .Where(d => d.CanWrite)
                .Select(d => (d, s: sourceProps.FirstOrDefault(s => s.Name == d.Name)))
                .Where(ds => ds.s != null)
                ;
            foreach (var (d, s) in propPairs)
            {
                d.SetValue(dest, s.GetValue(source));
            }

            var sourceFields = source.GetType().GetFields();
            var fieldPairs = dest.GetType().GetFields()
                .Select(d => (d, s: sourceFields.FirstOrDefault(s => s.Name == d.Name)))
                .Where(ds => ds.s != null)
                ;
            foreach (var (d, s) in fieldPairs)
            {
                d.SetValue(dest, s.GetValue(source));
            }
        }

        private void CreateList()
        {
            bool dragable = true, header = true, add = true, remove = true;
            this.List = new ReorderableList(this.Property.serializedObject, this.Property, dragable, header, add, remove);
            this.List.drawHeaderCallback += rect => this._property.isExpanded = EditorGUI.ToggleLeft(rect, this._property.displayName, this._property.isExpanded, EditorStyles.boldLabel);
            this.List.onCanRemoveCallback += list => this.List.count > 0;
            this.List.drawElementCallback += this.DrawElement;
            this.List.onAddCallback += list => {
                this._property.InsertArrayElementAtIndex(this._property.arraySize);
                var item = this._property.GetArrayElementAtIndex(this._property.arraySize - 1);
                this._property.serializedObject.ApplyModifiedProperties();
                if (item.propertyType == SerializedPropertyType.Generic)
                {
                    var (field, obj) = item.GetRealValue();
                    if (obj != null)
                    {
                        // Construct a proper new instance
                        try
                        {
                            object newInstance = Activator.CreateInstance(obj.GetType());
                            CopyValues(newInstance, obj);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Couldn't instance {obj.GetType().Name} to use for default values: {ex.Message}");
                        }
                    }
                }
            };
            this.List.elementHeightCallback += idx => Mathf.Max(EditorGUIUtility.singleLineHeight, EditorGUI.GetPropertyHeight(this._property.GetArrayElementAtIndex(idx), GUIContent.none, true) + 4.0f);
        }

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            if (this._property.GetArrayElementAtIndex(index).propertyType == SerializedPropertyType.Generic)
            {
                EditorGUI.LabelField(new Rect(rect.x + 16, rect.y, rect.width - 16, EditorGUIUtility.singleLineHeight), this._property.GetArrayElementAtIndex(index).displayName);
            }
            rect.height = EditorGUI.GetPropertyHeight(this._property.GetArrayElementAtIndex(index), GUIContent.none, true);
            rect.y += 1;
            rect.x += 12;
            rect.width -= 12;
            EditorGUI.PropertyField(rect, this._property.GetArrayElementAtIndex(index), GUIContent.none, true);
            this.List.elementHeight = rect.height + 4.0f;
        }
    }
    #endregion
}