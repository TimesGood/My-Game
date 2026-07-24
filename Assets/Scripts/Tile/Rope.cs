using System.Collections.Generic;
using UnityEngine;

//藤蔓

public class Rope : MonoBehaviour
{
    private TilemapManager tilemap => TilemapManager.Instance;
    public Rigidbody2D hook;//连接点
    public GameObject prefabRopeSeg;
    public int numLinks = 5;
    public HingeJoint2D top;

    private void Start() {
        GenerateRope();
    }


    private void GenerateRope() {
        Rigidbody2D prevBod = hook;

        //查看这个位置的藤蔓数据
        AddonLayer layer = tilemap.GetLayer(LayerType.Addons) as AddonLayer;
        Vector3Int pos = layer._tilemap.WorldToCell(transform.position);
        int growthData = layer.GetGrowthData(pos);
        if (growthData != 0) numLinks = growthData;

        for (int i = 0; i < numLinks; i++) {
            //int index = Random.Range(0, prefabRopeSegs.Length);
            //GameObject newSeg = Instantiate(prefabRopeSegs[index]);
            GameObject newSeg = Instantiate(prefabRopeSeg);
            newSeg.transform.parent = null;
            newSeg.transform.position = transform.position;
            //链接
            HingeJoint2D hj = newSeg.GetComponent<HingeJoint2D>();
            hj.autoConfigureConnectedAnchor = false;
            hj.connectedBody = prevBod;
            prevBod = newSeg.GetComponent<Rigidbody2D>();

            //顶端
            if (i == 0) {
                top = hj;
            }

            //末尾
            if (i == numLinks - 1) {
                newSeg.GetComponent<RopeSegment>().isEnd = true;
            }
        }
    }

    /// <summary>
    /// 添加铰链（头插）
    /// </summary>
    [ContextMenu("AddLink")]
    public void AddLink() {
        GameObject newSeg = Instantiate(prefabRopeSeg);
        newSeg.transform.parent = null;
        newSeg.transform.position = transform.position;

        HingeJoint2D hj = newSeg.GetComponent<HingeJoint2D>();
        hj.connectedBody = hook;
        newSeg.GetComponent<RopeSegment>().connectedBelow = top.gameObject;
        top.connectedBody = newSeg.GetComponent<Rigidbody2D>();
        top.GetComponent<RopeSegment>().ResetAnchor();
        top = hj;
    }

    [ContextMenu("RemoveLink")]
    public void RemoveLink() {
        HingeJoint2D newTop = top.gameObject.GetComponent<RopeSegment>().connectedBelow.GetComponent<HingeJoint2D>();
        newTop.connectedBody = hook;
        newTop.gameObject.transform.position = hook.gameObject.transform.position;
        newTop.GetComponent<RopeSegment>().ResetAnchor();
        Destroy(top.gameObject);
        top = newTop;
    }
}
