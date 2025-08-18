using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player inst;

    public bool isMove = false;
    public float moveSpeed = 8f; // 이동 속도
    public Ease DG_Ease = Ease.Linear;
    public Animator animator;

    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
        }
    }
    
    public void SetAnimation(string aniName)
    {
        animator.SetTrigger(aniName);
    }

    public void GotoPosition(List<Vector3> postions)
    {
        StartCoroutine(MovePosition(postions));
    }

    public IEnumerator MovePosition(List<Vector3> postions)
    {
        if (postions.Count == 0) yield break;

        isMove = true;

        // 애니메이션 트리거
        animator.SetTrigger("walk");

        foreach (Vector3 targetPosition in postions)
        {
            // 회전 방향 계산
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.DORotateQuaternion(targetRotation, 1f); // 0.2초 정도로 회전
            }

            
            // 거리 = 속도 시간
            // DOTween으로 이동
            bool moveDone = false;

            float distance = Vector3.Distance(transform.position, targetPosition);

            float DG_Time = distance / moveSpeed;

            transform.DOMove(targetPosition, DG_Time)
                     .SetEase(DG_Ease)
                     .OnComplete(() => moveDone = true);

            // 이동 완료될 때까지 대기
            yield return new WaitUntil(() => moveDone);
        }

        animator.SetTrigger("idle");
        isMove = false;
    }
}
