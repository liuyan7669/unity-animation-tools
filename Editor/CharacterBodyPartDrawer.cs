using UnityEditor;
using Cowart.InspectorGroups.Editor;
using UnityEngine;

namespace Cowart.AnimatorPlayer.Editor
{
/// <summary>
/// 在 Inspector 中以人体骨骼名称绘制 CharacterBodyParts.BodyPart。
/// </summary>
[CustomPropertyDrawer(typeof(CharacterBodyParts.BodyPart))]
public class CharacterBodyPartDrawer : InspectorGroupsPropertyDrawer
{
    protected override GUIContent GetLabel(SerializedProperty property, GUIContent label)
    {
        var bone = property.FindPropertyRelative("bodyBone");
        if (bone == null || bone.propertyType != SerializedPropertyType.Enum) return label;
        return new GUIContent(GetBodyBoneName((HumanBodyBones)bone.intValue), label.tooltip);
    }

    private static string GetBodyBoneName(HumanBodyBones bodyBone)
    {
        string name = bodyBone.ToString();
        string side = string.Empty;
        if (name.StartsWith("Left"))
        {
            side = "左";
            name = name.Substring(4);
        }
        else if (name.StartsWith("Right"))
        {
            side = "右";
            name = name.Substring(5);
        }

        switch (name)
        {
            case "Hips": return "骨盆";
            case "Spine": return "脊柱";
            case "Chest": return "胸部";
            case "UpperChest": return "上胸部";
            case "Neck": return "颈部";
            case "Head": return "头部";
            case "Jaw": return "下颌";
            case "UpperLeg": return side + "大腿";
            case "LowerLeg": return side + "小腿";
            case "Foot": return side + "脚";
            case "Toes": return side + "脚趾";
            case "Shoulder": return side + "肩部";
            case "UpperArm": return side + "上臂";
            case "LowerArm": return side + "前臂";
            case "Hand": return side + "手";
            case "Eye": return side + "眼睛";
        }

        return side + name
            .Replace("Thumb", "拇指")
            .Replace("Index", "食指")
            .Replace("Middle", "中指")
            .Replace("Ring", "无名指")
            .Replace("Little", "小指")
            .Replace("Proximal", "近节")
            .Replace("Intermediate", "中节")
            .Replace("Distal", "远节");
    }
}
}
