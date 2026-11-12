using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cowart.AnimatorPlayer
{
    /// <summary>
    /// 记录运行时动画事件及其回调。
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.AnimationTools", sourceAssembly: "Cowart.AnimationTools", sourceClassName: "AnimatorAction")]
    public class AnimatorAction
    {
        public string clipName;
        public string actionKey;
        public Action action;
        public float actionTriggerSecond;

        /// <summary>
        /// 初始化动画事件数据。
        /// </summary>
        /// <param name="clipName">事件所属的动画片段名称。</param>
        /// <param name="actionKey">用于标识事件的唯一键。</param>
        /// <param name="action">事件触发时执行的回调。</param>
        /// <param name="actionTriggerSecond">事件在动画片段中的触发时间，单位为秒。</param>
        public AnimatorAction(string clipName, string actionKey, Action action, float actionTriggerSecond)
        {
            this.clipName = clipName;
            this.actionKey = actionKey;
            this.action = action;
            this.actionTriggerSecond = actionTriggerSecond;
        }
    }

    /// <summary>
    /// 提供动画播放、暂停、序列播放、重复播放和运行时事件管理能力。
    /// 可通过重写 virtual 成员扩展具体项目行为。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-2000)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Cowart.AnimationTools", sourceAssembly: "Cowart.AnimationTools", sourceClassName: "AnimatorPlayer")]
    public class AnimatorPlayer : MonoBehaviour
    {
        private const string AnimationEventCallbackName = "AnimationClipEventCallback";
        private const float LastFrameNormalizedTime = 0.9999f;

        /// <summary>
        /// 当前是否处于 Animator 回放模式。
        /// </summary>
        private bool isPlaybackMode;

        /// <summary>
        /// 所有动画片段事件列表。
        /// </summary>
        protected List<AnimatorAction> actions = new List<AnimatorAction>();

        private readonly List<AnimatorClipInfo> currentClipInfos = new List<AnimatorClipInfo>();

        /// <summary>
        /// 在组件唤醒时初始化动画行为。
        /// </summary>
        protected virtual void Awake()
        {
            Initialization();
        }
        /// <summary>
        /// 在组件禁用时退出回放模式，恢复 Animator 的正常更新状态。
        /// </summary>
        protected virtual void OnDisable()
        {
            ExitPlaybackMode();
        }

        private Animator animatorComponent;

        /// <summary>
        /// 当前物体上的 Animator；不存在时自动添加。
        /// </summary>
        public Animator AnimatorComponent
        {
            get
            {
                if (animatorComponent == null)
                {
                    animatorComponent = GetComponent<Animator>();
                    if (animatorComponent == null)
                    {
                        animatorComponent = gameObject.AddComponent<Animator>();
                    }
                }

                return animatorComponent;
            }
        }

        /// <summary>
        /// 设置动画播放速度。
        /// </summary>
        public float PlaySpeed
        {
            get { return AnimatorComponent.speed; }
            set { AnimatorComponent.speed = value; }
        }

        /// <summary>
        /// 暂停当前正在播放的动画（非设置Animator.speed模式）。
        /// </summary>
        public virtual bool IsPause
        {
            get { return isPlaybackMode; }
            set
            {
                if (isPlaybackMode == value)
                {
                    return;
                }

                if (value)
                {
                    AnimatorComponent.StartPlayback();
                }
                else
                {
                    AnimatorComponent.StopPlayback();
                }

                isPlaybackMode = value;
            }
        }

        /// <summary>
        /// 清空已登记的运行时动画事件数据。
        /// </summary>
        public virtual void Initialization()
        {
            actions.Clear();
        }

        /// <summary>
        /// 从起始位置播放指定动画状态。
        /// </summary>
        /// <param name="stateName">要播放的动画状态名称；为空时直接执行结束回调。</param>
        /// <param name="playEnd">动画播放完成时执行的回调。</param>
        /// <param name="layerIndex">动画状态所在的 Animator 层索引。</param>
        public virtual void Play(string stateName, Action playEnd = null, int layerIndex = 0)
        {
            ExitPlaybackMode();
            StopAllCoroutines();
            if (string.IsNullOrEmpty(stateName))
            {
                playEnd?.Invoke();
                return;
            }
            AnimatorComponent.Play(stateName, layerIndex, 0f);
            if (playEnd != null)
            {
                StartCoroutine(ListenerAnimatorPlayState(playEnd, layerIndex));
            }
        }

        /// <summary>
        /// 从指定归一化时间开始播放动画状态。
        /// </summary>
        /// <param name="stateName">要播放的动画状态名称；为空时直接执行结束回调。</param>
        /// <param name="normalizedTime">动画的起始归一化时间。</param>
        /// <param name="playEnd">动画播放完成时执行的回调。</param>
        /// <param name="layerIndex">动画状态所在的 Animator 层索引。</param>
        public virtual void Play(string stateName, float normalizedTime, Action playEnd = null, int layerIndex = 0)
        {
            ExitPlaybackMode();
            StopAllCoroutines();
            if (string.IsNullOrEmpty(stateName))
            {
                playEnd?.Invoke();
                return;
            }
            AnimatorComponent.Play(stateName, layerIndex, normalizedTime);
            if (playEnd != null)
            {
                StartCoroutine(ListenerAnimatorPlayState(playEnd, layerIndex));
            }
        }

        /// <summary>
        /// 从列表中随机选择一个有效动画状态并从起始位置播放。
        /// </summary>
        /// <param name="stateNames">可供随机选择的动画状态名称列表。</param>
        /// <param name="playEnd">随机选中的动画播放完成时执行的回调；列表无有效状态时立即执行。</param>
        /// <param name="layerIndex">动画状态所在的 Animator 层索引。</param>
        /// <returns>随机选中的动画在原列表中的索引；没有有效状态时返回 -1。</returns>
        public virtual int PlayRandom(List<string> stateNames, Action playEnd = null, int layerIndex = 0)
        {
            if (stateNames == null || stateNames.Count == 0)
            {
                Play(null, playEnd, layerIndex);
                return -1;
            }

            int validStateCount = 0;
            for (int i = 0; i < stateNames.Count; i++)
            {
                if (!string.IsNullOrEmpty(stateNames[i]))
                {
                    validStateCount++;
                }
            }

            if (validStateCount == 0)
            {
                Play(null, playEnd, layerIndex);
                return -1;
            }

            int randomIndex = UnityEngine.Random.Range(0, validStateCount);
            for (int i = 0; i < stateNames.Count; i++)
            {
                string stateName = stateNames[i];
                if (string.IsNullOrEmpty(stateName))
                {
                    continue;
                }

                if (randomIndex == 0)
                {
                    Play(stateName, playEnd, layerIndex);
                    return i;
                }

                randomIndex--;
            }

            return -1;
        }

        /// <summary>
        /// 随机且不重复地播放列表中的全部有效动画状态。
        /// </summary>
        /// <param name="stateNames">要随机播放的动画状态名称列表；方法不会修改该列表。</param>
        /// <param name="playEnd">每个随机动画播放完成时执行的回调，参数为该动画的状态名称。</param>
        /// <param name="allFinish">全部有效动画播放完成时执行的回调。</param>
        /// <param name="layerIndex">动画状态所在的 Animator 层索引。</param>
        public virtual void PlayRandomList(List<string> stateNames, Action<string> playEnd, Action allFinish, int layerIndex = 0)
        {
            ExitPlaybackMode();
            StopAllCoroutines();
            if (stateNames == null || stateNames.Count == 0)
            {
                allFinish?.Invoke();
                return;
            }

            List<string> remainingStateNames = new List<string>(stateNames.Count);
            for (int i = 0; i < stateNames.Count; i++)
            {
                string stateName = stateNames[i];
                if (!string.IsNullOrEmpty(stateName))
                {
                    remainingStateNames.Add(stateName);
                }
            }

            PlayNextRandomState(remainingStateNames, playEnd, allFinish, layerIndex);
        }

        /// <summary>
        /// 从剩余动画状态副本中随机抽取并播放下一项。
        /// </summary>
        private void PlayNextRandomState(
            List<string> remainingStateNames,
            Action<string> playEnd,
            Action allFinish,
            int layerIndex)
        {
            if (remainingStateNames.Count == 0)
            {
                allFinish?.Invoke();
                return;
            }

            int randomIndex = UnityEngine.Random.Range(0, remainingStateNames.Count);
            string stateName = remainingStateNames[randomIndex];
            remainingStateNames.RemoveAt(randomIndex);

            AnimatorComponent.Play(stateName, layerIndex, 0f);
            StartCoroutine(ListenerAnimatorPlayState(
                () =>
                {
                    playEnd?.Invoke(stateName);
                    PlayNextRandomState(remainingStateNames, playEnd, allFinish, layerIndex);
                },
                layerIndex));
        }

        /// <summary>
        /// 从指定索引开始，按列表顺序依次播放动画状态。
        /// </summary>
        /// <param name="startIndex">开始播放的列表索引。</param>
        /// <param name="stateNames">按播放顺序排列的动画状态名称列表。</param>
        /// <param name="playEnd">单个动画播放完成时执行的回调，参数为该动画的列表索引。</param>
        /// <param name="allFinish">列表为空、索引无效或列表中的动画全部处理完成时执行的回调。</param>
        /// <param name="layerIndex">动画状态所在的 Animator 层索引。</param>
        public virtual void PlayList(int startIndex, List<string> stateNames, Action<int> playEnd, Action allFinish, int layerIndex = 0)
        {
            ExitPlaybackMode();
            StopAllCoroutines();
            if (stateNames == null || stateNames.Count == 0)
            {
                allFinish?.Invoke();
                return;
            }
            if (startIndex < 0 || startIndex >= stateNames.Count)
            {
                allFinish?.Invoke();
                return;
            }
            string stateName = stateNames[startIndex];
            if (string.IsNullOrEmpty(stateName))
            {
                playEnd?.Invoke(startIndex);
                startIndex++;
                PlayList(startIndex, stateNames, playEnd, allFinish, layerIndex);
                return;
            }
            AnimatorComponent.Play(stateName, layerIndex, 0);
            StartCoroutine(ListenerAnimatorPlayState(() =>
            {
                playEnd?.Invoke(startIndex);
                startIndex++;
                PlayList(startIndex, stateNames, playEnd, allFinish, layerIndex);
            }, layerIndex));
        }

        /// <summary>
        /// 从指定索引和归一化时间开始，按列表顺序依次播放动画状态。
        /// </summary>
        /// <param name="startIndex">开始播放的列表索引。</param>
        /// <param name="stateNames">按播放顺序排列的动画状态名称列表。</param>
        /// <param name="normalizedTime">每个动画的起始归一化时间。</param>
        /// <param name="playEnd">单个动画播放完成时执行的回调，参数为该动画的列表索引。</param>
        /// <param name="allFinish">列表为空、索引无效或列表中的动画全部处理完成时执行的回调。</param>
        /// <param name="layerIndex">动画状态所在的 Animator 层索引。</param>
        public virtual void PlayList(int startIndex, List<string> stateNames, float normalizedTime, Action<int> playEnd, Action allFinish, int layerIndex = 0)
        {
            ExitPlaybackMode();
            StopAllCoroutines();
            if (stateNames == null || stateNames.Count == 0)
            {
                allFinish?.Invoke();
                return;
            }
            if (startIndex < 0 || startIndex >= stateNames.Count)
            {
                allFinish?.Invoke();
                return;
            }
            string stateName = stateNames[startIndex];
            if (string.IsNullOrEmpty(stateName))
            {
                playEnd?.Invoke(startIndex);
                startIndex++;
                PlayList(startIndex, stateNames, normalizedTime, playEnd, allFinish, layerIndex);
                return;
            }
            AnimatorComponent.Play(stateName, layerIndex, normalizedTime);
            StartCoroutine(ListenerAnimatorPlayState(() =>
            {
                playEnd?.Invoke(startIndex);
                startIndex++;
                PlayList(startIndex, stateNames, normalizedTime, playEnd, allFinish, layerIndex);
            }, layerIndex));
        }

        /// <summary>
        /// 按指定次数重复播放动画状态。
        /// </summary>
        /// <param name="stateName">要重复播放的动画状态名称；为空时直接执行结束回调。</param>
        /// <param name="whileCount">剩余播放次数；小于或等于 0 时直接结束。</param>
        /// <param name="playEnd">全部重复播放结束时执行的回调。</param>
        /// <param name="playStartOne">每次开始播放时执行的回调，参数为当前剩余播放次数。</param>
        /// <param name="playEndOnce">每次播放完成时执行的回调，参数为当前剩余播放次数。</param>
        /// <param name="layerIndex">动画状态所在的 Animator 层索引。</param>
        public virtual void PlayWhile(string stateName, int whileCount, Action playEnd, Action<int> playStartOne = null, Action<int> playEndOnce = null, int layerIndex = 0)
        {
            ExitPlaybackMode();
            StopAllCoroutines();
            if (string.IsNullOrEmpty(stateName))
            {
                playEnd?.Invoke();
                return;
            }
            if (whileCount <= 0)
            {
                playEnd?.Invoke();
                return;
            }
            AnimatorComponent.Play(stateName, layerIndex, 0);
            playStartOne?.Invoke(whileCount);
            StartCoroutine(ListenerAnimatorPlayState(() =>
            {
                playEndOnce?.Invoke(whileCount);
                whileCount--;
                PlayWhile(stateName, whileCount, playEnd, playStartOne, playEndOnce, layerIndex);
            }, layerIndex));
        }

        /// <summary>
        /// 通过回放模式将指定动画状态固定在起始位置，不修改 Animator 的播放速度。
        /// </summary>
        /// <param name="stateName">要固定的动画状态名称。</param>
        /// <param name="layerIndex">动画状态所在的 Animator 层索引。</param>
        public virtual void FixedToFirstFrame(string stateName, int layerIndex = 0)
        {
            StopAllCoroutines();
            AnimatorComponent.Play(stateName, layerIndex, 0f);
            AnimatorComponent.StartPlayback();
            isPlaybackMode = true;
        }

        /// <summary>
        /// 通过回放模式将指定动画状态固定在给定的归一化时间，不修改 Animator 的播放速度。
        /// </summary>
        /// <param name="stateName">要固定的动画状态名称。</param>
        /// <param name="normalizedTime">要固定到的归一化时间。</param>
        /// <param name="layerIndex">动画状态所在的 Animator 层索引。</param>
        public virtual void FixedToNormalizedTime(string stateName, float normalizedTime, int layerIndex = 0)
        {
            StopAllCoroutines();
            AnimatorComponent.Play(stateName, layerIndex, normalizedTime);
            AnimatorComponent.StartPlayback();
            isPlaybackMode = true;
        }

        /// <summary>
        /// 通过回放模式将指定动画状态固定在结束位置附近，不修改 Animator 的播放速度。
        /// </summary>
        /// <param name="stateName">要固定的动画状态名称。</param>
        /// <param name="layerIndex">动画状态所在的 Animator 层索引。</param>
        public virtual void FixedToLastFrame(string stateName, int layerIndex = 0)
        {
            StopAllCoroutines();
            AnimatorComponent.Play(stateName, layerIndex, LastFrameNormalizedTime);
            AnimatorComponent.StartPlayback();
            isPlaybackMode = true;
        }

        /// <summary>
        /// 在运行时向指定动画片段添加事件。
        /// </summary>
        /// <param name="clipName">要绑定事件的动画片段名称。</param>
        /// <param name="actionKey">动画片段内用于标识事件的唯一键。</param>
        /// <param name="action">事件触发时执行的回调。</param>
        /// <param name="actionTriggerSecond">事件触发的时间（单位秒）。</param>
        /// <param name="forceReplace">存在同键事件时是否替换其回调和触发时间。</param>
        public virtual void AddEvent(string clipName, string actionKey, Action action, float actionTriggerSecond, bool forceReplace = false)
        {
            AnimatorAction findAction = actions.Find(x => x.clipName == clipName && x.actionKey == actionKey);
            if (findAction == null)
            {
                actions.Add(new AnimatorAction(clipName, actionKey, action, actionTriggerSecond));
                BindAnimationClipEvent(clipName, actionKey, actionTriggerSecond);
            }
            else
            {
                if (forceReplace)
                {
                    findAction.action = action;
                    findAction.actionTriggerSecond = actionTriggerSecond;
                    ReplaceAnimationClipEvent(clipName, actionKey, actionTriggerSecond);
                }
                else
                {
                    UnityEngine.Debug.LogError($"事件【{actionKey}】已存在，无法添加事件。");
                }
            }
        }

        /// <summary>
        /// 移除运行时添加到指定动画片段的事件。
        /// </summary>
        /// <param name="clipName">事件所属的动画片段名称。</param>
        /// <param name="actionKey">要移除的事件键。</param>
        public virtual void RemoveEvent(string clipName, string actionKey)
        {
            actions.RemoveAll(x => x.clipName == clipName && x.actionKey == actionKey);

            if (TryGetAnimationClip(clipName, out AnimationClip clip))
            {
                RemoveAnimationClipEvents(
                    clip,
                    animationEvent => animationEvent.functionName == AnimationEventCallbackName &&
                                      animationEvent.stringParameter == actionKey);
            }
        }

        /// <summary>
        /// 清除运行时添加到指定动画片段的所有事件。
        /// </summary>
        /// <param name="clipName">要清除事件的动画片段名称。</param>
        public virtual void ClearEvent(string clipName)
        {
            actions.RemoveAll(x => x.clipName == clipName);

            if (TryGetAnimationClip(clipName, out AnimationClip clip))
            {
                RemoveAnimationClipEvents(clip, animationEvent => animationEvent.functionName == AnimationEventCallbackName);
            }
        }

        /// <summary>
        /// 清除运行时添加到当前 Animator 所有动画片段的事件。
        /// </summary>
        public virtual void ClearEvent()
        {
            actions.Clear();

            RuntimeAnimatorController runtimeController = AnimatorComponent.runtimeAnimatorController;
            if (runtimeController == null)
            {
                return;
            }

            AnimationClip[] clips = runtimeController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                RemoveAnimationClipEvents(clips[i], animationEvent => animationEvent.functionName == AnimationEventCallbackName);
            }
        }

        /// <summary>
        /// 手动调用通过 <see cref="AddEvent"/> 添加的指定事件。
        /// </summary>
        /// <param name="clipName">事件所属的动画片段名称。</param>
        /// <param name="actionKey">要调用的事件键。</param>
        public virtual void ManuallyCallEvent(string clipName, string actionKey)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                AnimatorAction animatorAction = actions[i];
                if (animatorAction.clipName == clipName && animatorAction.actionKey == actionKey)
                {
                    animatorAction.action?.Invoke();
                }
            }
        }

        /// <summary>
        /// 更新 Animator 后获取指定层当前动画片段的信息。
        /// </summary>
        /// <param name="layerIndex">要查询的 Animator 层索引。</param>
        /// <returns>当前动画片段的信息；不存在动画片段时返回默认值。</returns>
        public AnimatorClipInfo GetCurrentClip(int layerIndex = 0)
        {
            AnimatorComponent.Update(0f);
            AnimatorComponent.GetCurrentAnimatorClipInfo(layerIndex, currentClipInfos);
            if (currentClipInfos.Count > 0)
            {
                return currentClipInfos[0];
            }

            return default;
        }
        /// <summary>
        /// 更新 Animator 后获取指定层当前动画状态的信息。
        /// </summary>
        /// <param name="layerIndex">要查询的 Animator 层索引。</param>
        /// <returns>当前动画状态信息，包括归一化播放时间等数据。</returns>
        public AnimatorStateInfo GetCurrentState(int layerIndex = 0)
        {
            AnimatorComponent.Update(0f);
            return AnimatorComponent.GetCurrentAnimatorStateInfo(layerIndex);
        }

        /// <summary>
        /// 等待指定层当前动画状态首次播放完成，然后执行回调。
        /// </summary>
        /// <param name="playEnd">动画播放完成时执行的回调。</param>
        /// <param name="layerIndex">要监听的 Animator 层索引。</param>
        /// <returns>用于等待动画播放完成的协程枚举器。</returns>
        protected virtual IEnumerator ListenerAnimatorPlayState(Action playEnd, int layerIndex)
        {
            yield return null;

            while (true)
            {
                AnimatorStateInfo stateInfo = GetCurrentState(layerIndex);
                if (stateInfo.normalizedTime >= 1)
                {
                    break;
                }
                yield return null;
            }

            playEnd?.Invoke();
        }
        /// <summary>
        /// 如果当前处于回放模式，则停止回放并恢复 Animator 的正常更新状态。
        /// </summary>
        protected virtual void ExitPlaybackMode()
        {
            if (isPlaybackMode)
            {
                AnimatorComponent.StopPlayback();
                isPlaybackMode = false;
            }
        }
        /// <summary>
        /// 将运行时事件绑定到指定动画片段。
        /// </summary>
        /// <param name="clipName">要绑定事件的动画片段名称。</param>
        /// <param name="actionKey">传递给事件回调的事件键。</param>
        /// <param name="actionTriggerSecond">事件在动画片段中的触发时间，单位为秒。</param>
        protected virtual void BindAnimationClipEvent(string clipName, string actionKey, float actionTriggerSecond)
        {
            if (!TryGetAnimationClip(clipName, out AnimationClip clip))
            {
                return;
            }

            AnimationEvent animationEvent = new AnimationEvent
            {
                messageOptions = SendMessageOptions.DontRequireReceiver,
                functionName = AnimationEventCallbackName,
                stringParameter = actionKey,
                time = actionTriggerSecond
            };
            clip.AddEvent(animationEvent);
        }
        /// <summary>
        /// 更新指定动画片段中已绑定事件的触发时间。
        /// </summary>
        /// <param name="clipName">事件所属的动画片段名称。</param>
        /// <param name="actionKey">要更新的事件键。</param>
        /// <param name="actionTriggerSecond">新的触发时间，单位为秒。</param>
        protected virtual void ReplaceAnimationClipEvent(string clipName, string actionKey, float actionTriggerSecond)
        {
            if (!TryGetAnimationClip(clipName, out AnimationClip clip))
            {
                return;
            }

            AnimationEvent[] events = clip.events;
            for (int i = 0; i < events.Length; i++)
            {
                AnimationEvent animationEvent = events[i];
                if (animationEvent.functionName == AnimationEventCallbackName && animationEvent.stringParameter == actionKey)
                {
                    animationEvent.time = actionTriggerSecond;
                    clip.events = events;
                    return;
                }
            }
        }

        /// <summary>
        /// 按名称查找当前 Animator Controller 中的动画片段。
        /// </summary>
        private bool TryGetAnimationClip(string clipName, out AnimationClip result)
        {
            result = null;
            RuntimeAnimatorController runtimeController = AnimatorComponent.runtimeAnimatorController;
            if (runtimeController == null || string.IsNullOrEmpty(clipName))
            {
                return false;
            }

            AnimationClip[] clips = runtimeController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip != null && clip.name == clipName)
                {
                    result = clip;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 删除动画片段中符合条件的事件。
        /// </summary>
        private static void RemoveAnimationClipEvents(AnimationClip clip, Predicate<AnimationEvent> predicate)
        {
            if (clip == null || predicate == null)
            {
                return;
            }

            AnimationEvent[] events = clip.events;
            if (events.Length == 0)
            {
                return;
            }

            List<AnimationEvent> remainingEvents = new List<AnimationEvent>(events.Length);
            for (int i = 0; i < events.Length; i++)
            {
                if (!predicate(events[i]))
                {
                    remainingEvents.Add(events[i]);
                }
            }

            if (remainingEvents.Count != events.Length)
            {
                clip.events = remainingEvents.ToArray();
            }
        }

        /// <summary>
        /// 接收 AnimationClip 事件并执行与事件键关联的运行时回调。
        /// </summary>
        /// <param name="actionKey">AnimationEvent 传入的事件键。</param>
        protected virtual void AnimationClipEventCallback(string actionKey)
        {
            AnimatorAction findAction = actions.Find(x => x.actionKey == actionKey);
            if (findAction != null)
            {
                findAction.action?.Invoke();
            }
        }

    }
}
