using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    public bool isMove = false;
    public List<Vector3> ls_MovePosition = new List<Vector3>();
    public float DG_Time = 0.75f;
    public Ease DG_Ease = Ease.Linear;
    public Animator animator;

    [ContextMenu("move")]
    public void GotoPosition()
    {
        StartCoroutine(MovePosition());
    }

    public IEnumerator MovePosition()
    {
        if (ls_MovePosition.Count == 0) yield break;

        isMove = true;

        // 애니메이션 트리거
        animator.SetTrigger("walk");

        foreach (Vector3 targetPosition in ls_MovePosition)
        {
            // 회전 방향 계산
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.DORotateQuaternion(targetRotation, 1f); // 0.2초 정도로 회전
            }

            

            // DOTween으로 이동
            bool moveDone = false;
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
