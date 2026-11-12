using System;
using DG.Tweening;
using UnityEngine;

namespace Cowart.AnimatorPlayer.DOTween
{
/// <summary>
/// 使用 DOTween 按指定速度控制物体的位置和旋转。
/// </summary>
[DisallowMultipleComponent]
public class TransformTweenController : MonoBehaviour
{
    private const float PathLookAhead = 0.01f;

    /// <summary>
    /// 移动速度，单位为世界单位/秒。
    /// </summary>
    public float moveSpeed = 1f;

    /// <summary>
    /// 旋转速度，单位为度/秒。
    /// </summary>
    public float rotateSpeed = 90f;

    /// <summary>
    /// 当前正在执行的移动补间。
    /// </summary>
    protected Tween moveTween;

    /// <summary>
    /// 当前正在执行的旋转补间。
    /// </summary>
    protected Tween rotateTween;

    /// <summary>
    /// 以恒定速度移动到目标位置。
    /// </summary>
    /// <param name="target">提供目标世界坐标的变换。</param>
    /// <param name="onComplete">移动完成后执行的回调。</param>
    public virtual void MoveTo(Transform target, Action onComplete = null)
    {
        if (target == null)
        {
            return;
        }

        moveTween?.Kill();

        if (Vector3.Distance(transform.position, target.position) <= Mathf.Epsilon)
        {
            transform.position = target.position;
            onComplete?.Invoke();
            return;
        }

        moveTween = transform.DOMove(target.position, Mathf.Max(0.01f, moveSpeed))
            .SetSpeedBased()
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                moveTween = null;
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// 按数组顺序以恒定速度沿线性路径移动，并可自动朝向路径前方。
    /// </summary>
    /// <param name="pathPoints">依次经过的路径点；空引用会被忽略。</param>
    /// <param name="lookAtNextPoint">是否在移动过程中自动朝向下一个路径点。</param>
    /// <param name="onComplete">完成整段路径后执行的回调。</param>
    /// <param name="onPointMoveStart">开始前往单个路径点时执行的回调，参数为路径点及其原数组索引。</param>
    /// <param name="onPointMoveComplete">到达单个路径点时执行的回调，参数为路径点及其原数组索引。</param>
    public virtual void MoveAlongPath(
        Transform[] pathPoints,
        bool lookAtNextPoint = true,
        Action onComplete = null,
        Action<Transform, int> onPointMoveStart = null,
        Action<Transform, int> onPointMoveComplete = null)
    {
        if (pathPoints == null || pathPoints.Length == 0)
        {
            return;
        }

        Vector3[] positions = new Vector3[pathPoints.Length];
        Transform[] validPathPoints = new Transform[pathPoints.Length];
        int[] originalIndices = new int[pathPoints.Length];
        float[] cumulativeDistances = new float[pathPoints.Length];
        Vector3 previousPosition = transform.position;
        float pathLength = 0f;
        int validPointCount = 0;

        for (int i = 0; i < pathPoints.Length; i++)
        {
            Transform pathPoint = pathPoints[i];
            if (pathPoint == null)
            {
                continue;
            }

            Vector3 position = pathPoint.position;
            positions[validPointCount] = position;
            validPathPoints[validPointCount] = pathPoint;
            originalIndices[validPointCount] = i;
            pathLength += Vector3.Distance(previousPosition, position);
            cumulativeDistances[validPointCount] = pathLength;
            validPointCount++;
            previousPosition = position;
        }

        if (validPointCount == 0)
        {
            return;
        }

        if (validPointCount != positions.Length)
        {
            Array.Resize(ref positions, validPointCount);
            Array.Resize(ref validPathPoints, validPointCount);
            Array.Resize(ref originalIndices, validPointCount);
            Array.Resize(ref cumulativeDistances, validPointCount);
        }

        moveTween?.Kill();
        moveTween = null;

        if (pathLength <= Mathf.Epsilon)
        {
            for (int i = 0; i < validPointCount; i++)
            {
                onPointMoveStart?.Invoke(validPathPoints[i], originalIndices[i]);
                transform.position = positions[i];
                onPointMoveComplete?.Invoke(validPathPoints[i], originalIndices[i]);
            }

            onComplete?.Invoke();
            return;
        }

        var pathTween = transform.DOPath(
            positions,
            Mathf.Max(0.01f, moveSpeed),
            PathType.Linear,
            PathMode.Full3D);

        if (lookAtNextPoint)
        {
            pathTween.SetLookAt(PathLookAhead);
        }

        int currentPointIndex = 0;
        moveTween = pathTween
            .SetSpeedBased()
            .SetEase(Ease.Linear)
            .OnStart(() =>
            {
                onPointMoveStart?.Invoke(
                    validPathPoints[currentPointIndex],
                    originalIndices[currentPointIndex]);
            })
            .OnUpdate(() =>
            {
                float traveledDistance = pathTween.ElapsedPercentage() * pathLength;

                while (currentPointIndex < validPointCount &&
                       traveledDistance >= cumulativeDistances[currentPointIndex])
                {
                    onPointMoveComplete?.Invoke(
                        validPathPoints[currentPointIndex],
                        originalIndices[currentPointIndex]);
                    currentPointIndex++;

                    if (!pathTween.IsActive())
                    {
                        return;
                    }

                    if (currentPointIndex < validPointCount)
                    {
                        onPointMoveStart?.Invoke(
                            validPathPoints[currentPointIndex],
                            originalIndices[currentPointIndex]);

                        if (!pathTween.IsActive())
                        {
                            return;
                        }
                    }
                }
            })
            .OnComplete(() =>
            {
                moveTween = null;

                while (currentPointIndex < validPointCount)
                {
                    onPointMoveComplete?.Invoke(
                        validPathPoints[currentPointIndex],
                        originalIndices[currentPointIndex]);
                    currentPointIndex++;

                    if (currentPointIndex < validPointCount)
                    {
                        onPointMoveStart?.Invoke(
                            validPathPoints[currentPointIndex],
                            originalIndices[currentPointIndex]);
                    }
                }

                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// 以恒定角速度旋转到目标朝向。
    /// </summary>
    /// <param name="target">提供目标世界朝向的变换。</param>
    /// <param name="onComplete">旋转完成后执行的回调。</param>
    public virtual void RotateTo(Transform target, Action onComplete = null)
    {
        if (target == null)
        {
            return;
        }

        rotateTween?.Kill();

        if (Quaternion.Angle(transform.rotation, target.rotation) <= Mathf.Epsilon)
        {
            transform.rotation = target.rotation;
            onComplete?.Invoke();
            return;
        }

        rotateTween = transform.DORotate(target.eulerAngles, Mathf.Max(0.01f, rotateSpeed), RotateMode.Fast)
            .SetSpeedBased()
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                rotateTween = null;
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// 停止当前移动补间。
    /// </summary>
    public virtual void StopMove()
    {
        moveTween?.Kill();
        moveTween = null;
    }

    /// <summary>
    /// 停止当前旋转补间。
    /// </summary>
    public virtual void StopRotate()
    {
        rotateTween?.Kill();
        rotateTween = null;
    }

    private void OnDestroy()
    {
        StopMove();
        StopRotate();
    }
}
}
