#if UNITY_EDITOR
using System;
using System.Linq;
using AES.Tools;          // TextBinding, StringTextBinding, IntTextBinding, FloatTextBinding, BoolTextBinding
using UnityEditor;
using UnityEngine;

namespace Databinding.Editor
{
    public static class TextBindingContextMenu
    {
        // ---------------- 공통 교체 로직 ----------------

        private static void ReplaceComponent(Component source, Type newType)
        {
            if (source == null || source.gameObject == null || newType == null)
                return;

            // 같은 타입이면 패스 (원하면 제거 가능)
            if (source.GetType() == newType)
                return;

            var go = source.gameObject;

            Undo.SetCurrentGroupName($"Change {source.GetType().Name} → {newType.Name}");
            int group = Undo.GetCurrentGroup();

            var srcSO = new SerializedObject(source);
            var newComp = Undo.AddComponent(go, newType);
            var dstSO = new SerializedObject(newComp);

            srcSO.Update();
            dstSO.Update();

            var prop = srcSO.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;

                // m_Script 복사 금지
                if (prop.name == "m_Script")
                    continue;

                var dstProp = dstSO.FindProperty(prop.propertyPath);
                if (dstProp == null)
                    continue;

                if (dstProp.propertyType != prop.propertyType)
                    continue;

                CopyPropertyValue(dstProp, prop);
            }

            dstSO.ApplyModifiedProperties();

            Undo.DestroyObjectImmediate(source);
            Undo.CollapseUndoOperations(group);
        }

        private static void CopyPropertyValue(SerializedProperty dst, SerializedProperty src)
        {
            switch (src.propertyType)
            {
                case SerializedPropertyType.Integer:
                    dst.intValue = src.intValue; break;
                case SerializedPropertyType.Boolean:
                    dst.boolValue = src.boolValue; break;
                case SerializedPropertyType.Float:
                    dst.floatValue = src.floatValue; break;
                case SerializedPropertyType.String:
                    dst.stringValue = src.stringValue; break;
                case SerializedPropertyType.Color:
                    dst.colorValue = src.colorValue; break;
                case SerializedPropertyType.ObjectReference:
                    dst.objectReferenceValue = src.objectReferenceValue; break;
                case SerializedPropertyType.LayerMask:
                    dst.intValue = src.intValue; break;
                case SerializedPropertyType.Enum:
                    dst.enumValueIndex = src.enumValueIndex; break;
                case SerializedPropertyType.Vector2:
                    dst.vector2Value = src.vector2Value; break;
                case SerializedPropertyType.Vector3:
                    dst.vector3Value = src.vector3Value; break;
                case SerializedPropertyType.Vector4:
                    dst.vector4Value = src.vector4Value; break;
                case SerializedPropertyType.Rect:
                    dst.rectValue = src.rectValue; break;
                case SerializedPropertyType.AnimationCurve:
                    dst.animationCurveValue = src.animationCurveValue; break;
                case SerializedPropertyType.Bounds:
                    dst.boundsValue = src.boundsValue; break;
                case SerializedPropertyType.Quaternion:
                    dst.quaternionValue = src.quaternionValue; break;
                // 필요하면 배열/Generic 타입 등 추가
            }
        }

        private static void ReplaceForSelection<TFrom>(MenuCommand command, Type toType)
            where TFrom : Component
        {
            var clicked = command.context as TFrom;
            if (clicked == null) return;

            var targets = Selection.gameObjects
                .SelectMany(go => go.GetComponents<TFrom>())
                .Cast<Component>()
                .ToList();

            if (!targets.Contains(clicked))
                targets.Add(clicked);

            foreach (var comp in targets.Distinct())
                ReplaceComponent(comp, toType);
        }

        // ---------------- TextBinding 컨텍스트 메뉴 ----------------
        // TextBinding 자체에 붙는 메뉴 (Auto + 다른 타입으로 변경)
        
        [MenuItem("CONTEXT/TextBinding/Change TextBinding Type/String")]
        private static void TextToString(MenuCommand command)
            => ReplaceForSelection<TextBinding>(command, typeof(StringTextBinding));

        [MenuItem("CONTEXT/TextBinding/Change TextBinding Type/Int")]
        private static void TextToInt(MenuCommand command)
            => ReplaceForSelection<TextBinding>(command, typeof(IntTextBinding));

        [MenuItem("CONTEXT/TextBinding/Change TextBinding Type/Float")]
        private static void TextToFloat(MenuCommand command)
            => ReplaceForSelection<TextBinding>(command, typeof(FloatTextBinding));

        [MenuItem("CONTEXT/TextBinding/Change TextBinding Type/Bool")]
        private static void TextToBool(MenuCommand command)
            => ReplaceForSelection<TextBinding>(command, typeof(BoolTextBinding));

        // ---------------- StringTextBinding 컨텍스트 메뉴 ----------------

        [MenuItem("CONTEXT/StringTextBinding/Change TextBinding Type/Auto")]
        private static void StringToAuto(MenuCommand command)
            => ReplaceForSelection<StringTextBinding>(command, typeof(TextBinding));
        

        [MenuItem("CONTEXT/StringTextBinding/Change TextBinding Type/Int")]
        private static void StringToInt(MenuCommand command)
            => ReplaceForSelection<StringTextBinding>(command, typeof(IntTextBinding));

        [MenuItem("CONTEXT/StringTextBinding/Change TextBinding Type/Float")]
        private static void StringToFloat(MenuCommand command)
            => ReplaceForSelection<StringTextBinding>(command, typeof(FloatTextBinding));

        [MenuItem("CONTEXT/StringTextBinding/Change TextBinding Type/Bool")]
        private static void StringToBool(MenuCommand command)
            => ReplaceForSelection<StringTextBinding>(command, typeof(BoolTextBinding));

        // ---------------- IntTextBinding 컨텍스트 메뉴 ----------------

        [MenuItem("CONTEXT/IntTextBinding/Change TextBinding Type/Auto")]
        private static void IntToAuto(MenuCommand command)
            => ReplaceForSelection<IntTextBinding>(command, typeof(TextBinding));

        [MenuItem("CONTEXT/IntTextBinding/Change TextBinding Type/String")]
        private static void IntToString(MenuCommand command)
            => ReplaceForSelection<IntTextBinding>(command, typeof(StringTextBinding));

        [MenuItem("CONTEXT/IntTextBinding/Change TextBinding Type/Float")]
        private static void IntToFloat(MenuCommand command)
            => ReplaceForSelection<IntTextBinding>(command, typeof(FloatTextBinding));

        [MenuItem("CONTEXT/IntTextBinding/Change TextBinding Type/Bool")]
        private static void IntToBool(MenuCommand command)
            => ReplaceForSelection<IntTextBinding>(command, typeof(BoolTextBinding));

        // ---------------- FloatTextBinding 컨텍스트 메뉴 ----------------

        [MenuItem("CONTEXT/FloatTextBinding/Change TextBinding Type/Auto")]
        private static void FloatToAuto(MenuCommand command)
            => ReplaceForSelection<FloatTextBinding>(command, typeof(TextBinding));

        [MenuItem("CONTEXT/FloatTextBinding/Change TextBinding Type/String")]
        private static void FloatToString(MenuCommand command)
            => ReplaceForSelection<FloatTextBinding>(command, typeof(StringTextBinding));

        [MenuItem("CONTEXT/FloatTextBinding/Change TextBinding Type/Int")]
        private static void FloatToInt(MenuCommand command)
            => ReplaceForSelection<FloatTextBinding>(command, typeof(IntTextBinding));

        [MenuItem("CONTEXT/FloatTextBinding/Change TextBinding Type/Bool")]
        private static void FloatToBool(MenuCommand command)
            => ReplaceForSelection<FloatTextBinding>(command, typeof(BoolTextBinding));

        // ---------------- BoolTextBinding 컨텍스트 메뉴 ----------------

        [MenuItem("CONTEXT/BoolTextBinding/Change TextBinding Type/Auto")]
        private static void BoolToAuto(MenuCommand command)
            => ReplaceForSelection<BoolTextBinding>(command, typeof(TextBinding));

        [MenuItem("CONTEXT/BoolTextBinding/Change TextBinding Type/String")]
        private static void BoolToString(MenuCommand command)
            => ReplaceForSelection<BoolTextBinding>(command, typeof(StringTextBinding));

        [MenuItem("CONTEXT/BoolTextBinding/Change TextBinding Type/Int")]
        private static void BoolToInt(MenuCommand command)
            => ReplaceForSelection<BoolTextBinding>(command, typeof(IntTextBinding));

        [MenuItem("CONTEXT/BoolTextBinding/Change TextBinding Type/Float")]
        private static void BoolToFloat(MenuCommand command)
            => ReplaceForSelection<BoolTextBinding>(command, typeof(FloatTextBinding));
    }
}
#endif
