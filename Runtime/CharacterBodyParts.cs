using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cowart.AnimationTools
{
/// <summary>
/// 保存 Humanoid 骨骼类型与场景节点之间的对应关系。
/// </summary>
[DisallowMultipleComponent]
public class CharacterBodyParts : MonoBehaviour
{
    /// <summary>
    /// 单个人体骨骼及其对应的 Transform。
    /// </summary>
    [Serializable]
    public class BodyPart
    {
        [SerializeField] private HumanBodyBones bodyBone;
        [SerializeField] private Transform target = null;

        /// <summary>
        /// 创建指定骨骼类型的配置项。
        /// </summary>
        /// <param name="bodyBone">Humanoid 骨骼类型。</param>
        public BodyPart(HumanBodyBones bodyBone)
        {
            this.bodyBone = bodyBone;
        }

        /// <summary>
        /// Humanoid 骨骼类型。
        /// </summary>
        public HumanBodyBones BodyBone => bodyBone;

        /// <summary>
        /// 骨骼对应的场景节点。
        /// </summary>
        public Transform Target => target;
    }

    [SerializeField] private List<BodyPart> parts = new List<BodyPart>();

    /// <summary>
    /// 获取面板中配置的全部人物部位。
    /// </summary>
    public IReadOnlyList<BodyPart> Parts => parts;

    /// <summary>
    /// 添加或重置组件时，为全部有效 Humanoid 骨骼创建默认配置项。
    /// </summary>
    private void Reset()
    {
        parts = new List<BodyPart>((int)HumanBodyBones.LastBone);
        foreach (HumanBodyBones bodyBone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bodyBone != HumanBodyBones.LastBone)
            {
                parts.Add(new BodyPart(bodyBone));
            }
        }
    }

    /// <summary>
    /// 根据人体骨骼类型获取对应节点，未找到时返回 null。
    /// </summary>
    /// <param name="bodyBone">需要查找的 Humanoid 骨骼类型。</param>
    /// <returns>配置的骨骼节点；未找到时返回 null。</returns>
    public Transform GetPart(HumanBodyBones bodyBone)
    {
        TryGetPart(bodyBone, out Transform target);
        return target;
    }

    /// <summary>
    /// 根据人体骨骼类型尝试获取对应节点。
    /// </summary>
    /// <param name="bodyBone">需要查找的 Humanoid 骨骼类型。</param>
    /// <param name="target">找到的骨骼节点。</param>
    /// <returns>找到有效节点时返回 true，否则返回 false。</returns>
    public bool TryGetPart(HumanBodyBones bodyBone, out Transform target)
    {
        target = null;
        if (parts == null)
        {
            return false;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            BodyPart part = parts[i];
            if (part != null && part.BodyBone == bodyBone && part.Target != null)
            {
                target = part.Target;
                return true;
            }
        }

        return false;
    }
}
}
