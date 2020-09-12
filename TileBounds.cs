using System;
using Unity.Mathematics;
using UnityEngine;

public class TileBounds : MonoBehaviour
{
    public BoundsCorner cornerId;
    public Vector3 boundsSize = new Vector3(5, 3, 5);

    private bool _drawCorners;
   
    public Vector3 CornerOffset => corners[(int)cornerId] * boundsSize;
    
    public Matrix4x4 BoundsLocalToWorld => Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one) *
                                           Matrix4x4.Translate(-corners[(int) cornerId] * boundsSize);

    public Bounds Bounds => new Bounds(BoundsLocalToWorld.MultiplyPoint(Vector3.zero), boundsSize);

    public Matrix4x4 BoxPointToWorldMatrix(BoundsCorner cid)
    {
        return BoundsLocalToWorld * Matrix4x4.Translate(corners[(int) cid] * boundsSize);
    }

    public void SetCornerPosition(BoundsCorner targetCorner , Vector3 worldPosition)
    {
        //get local position, of the target corner and translate it into world space
        var localOffset = (-corners[(int) cornerId] + corners[(int) targetCorner]) * boundsSize;
        var boundsCorner = transform.TransformPoint(localOffset);
        var delta = worldPosition - boundsCorner;

        transform.position += delta;

    }

    private void OnEnable()
    {
        OnValidate();
    }

    private void OnValidate()
    {
    }

    [Serializable]
    public enum BoundsCorner
    {
        TopFarLeft = 0,
        TopFarRight = 1,
        TopNearRight = 2,
        TopNearLeft = 3,
        CenterFarLeft = 4,
        CenterFarRight = 5,
        CenterNearRight = 6,
        CenterNearLeft = 7,
        BottomFarLeft = 8,
        BottomFarRight = 9,
        BottomNearRight = 10,
        BottomNearLeft = 11,
        TopFarCenter = 12,
        TopRightCenter = 13,
        TopNearCenter = 14,
        TopLeftCenter = 15,
        BottomFarCenter = 16,
        BottomRightCenter = 17,
        BottomNearCenter = 18,
        BottomLeftCenter = 19,
        TopCenter = 20,
        FarCenter = 21,
        RightCenter = 22,
        NearCenter = 23,
        LeftCenter = 24,
        BottomCenter = 25,
        Center = 26,
    }

    public static float3[] corners = new[]
    {
        new float3(-.5f, .5f, .5f), //TopFarLeft,
        new float3(.5f, .5f, .5f), //TopFarRight,
        new float3(.5f, .5f, -.5f), //TopNearRight,
        new float3(-.5f, .5f, -.5f), //TopNearLeft,
        new float3(-.5f, 0, .5f), //CenterFarLeft,
        new float3(.5f, 0, .5f), //CenterFarRight,
        new float3(.5f, 0, -.5f), //CenterNearRight,
        new float3(-.5f, 0, -.5f), //CenterNearLeft,
        new float3(-.5f, -.5f, .5f), //BottomFarLeft,
        new float3(.5f, -.5f, .5f), //BottomFarRight,
        new float3(.5f, -.5f, -.5f), //BottomNearRight,
        new float3(-.5f, -.5f, -.5f), //BottomNearLeft,
        new float3(0, .5f, .5f), //TopFarCenter,     
        new float3(.5f, .5f, 0), //TopRightCenter,   
        new float3(0, .5f, -.5f), //TopNearCenter,    
        new float3(-.5f, .5f, 0), //TopLeftCenter,    
        new float3(0, -.5f, .5f), //BottomFarCenter,
        new float3(.5f, -.5f, 0), //BottomRightCenter,
        new float3(0, -.5f, -.5f), //BottomNearCenter,
        new float3(-.5f, -.5f, 0), //BottomLeftCenter
        new float3(0, .5f, 0), //TopCenter,
        new float3(0, 0, .5f), //FarCenter,
        new float3(.5f, 0, 0), //RightCenter,
        new float3(0, 0, -.5f), //NearCenter,
        new float3(-.5f, 0, 0), //LeftCenter,
        new float3(0, -.5f, 0), //BottomCenter,
        new float3(0, 0, 0) //CenterCenter
    };


    private void OnDrawGizmosSelected()
    {
        
        var values = Enum.GetValues(typeof(BoundsCorner));
        var b = new Bounds(Vector3.zero, Vector3.one);

        if (_drawCorners)
        {
            foreach (BoundsCorner cid in values)
            {
                Gizmos.matrix = BoxPointToWorldMatrix(cid);
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
        }
        
        Gizmos.matrix = BoundsLocalToWorld;
        //Gizmos.color = new Color(1f, 0f, 0f, 0.22f);

        Gizmos.color = new Color(0.45f, 0.43f, 1f, 0.35f);
        Gizmos.DrawWireCube(CornerOffset, Vector3.one*.1f);
        Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
        Gizmos.DrawWireCube(Vector3.zero, boundsSize);
    }
    
}