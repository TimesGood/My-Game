using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeSegment : MonoBehaviour
{
    public GameObject connectedAbove, connectedBelow;
    private SpriteRenderer spriteRenderer;
    public List<Sprite> commonSprite = new List<Sprite>();
    public List<Sprite> endSprite = new List<Sprite>();//末端精灵
    public bool isEnd;

    private void Start() {
        ResetAnchor();
    }

    public void ResetAnchor() {
        //设置随机精灵
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (isEnd) {
            int spriteIndex = Random.Range(0, endSprite.Count);
            spriteRenderer.sprite = endSprite[spriteIndex];
        } else {
            int spriteIndex = Random.Range(0, commonSprite.Count);
            spriteRenderer.sprite = commonSprite[spriteIndex];
        }

        //绑定上一个连接点
        connectedAbove = GetComponent<HingeJoint2D>().connectedBody.gameObject;
        RopeSegment aboveSegment = connectedAbove.GetComponent<RopeSegment>();
        if (aboveSegment != null) {
            aboveSegment.connectedBelow = gameObject;
            float spriteBottom = connectedAbove.GetComponent<SpriteRenderer>().bounds.size.y;
            GetComponent<HingeJoint2D>().connectedAnchor = new Vector2(0, spriteBottom * -1);

        } else {
            GetComponent<HingeJoint2D>().connectedAnchor = new Vector2(0, 0);

        }
    }
}
