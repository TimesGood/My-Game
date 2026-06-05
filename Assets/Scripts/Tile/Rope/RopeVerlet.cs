using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RopeVerlet : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private int _numOfRopeSegments = 50;
    [SerializeField] private float _ropeSegmentLength = 0.225f;

    [Header("Physics")]
    [SerializeField] private Vector2 _gravicyForce = new Vector2(0f, -2f);
    [SerializeField] private float _dampingFactor = 0.98f;
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private float _collisionRadius = 0.1f;
    [SerializeField] private float _bounceFactor = 0.1f;
    [SerializeField] private float _correctionClampAmount = 0.1f;

    [Header("Constraints")]
    [SerializeField] private int _numOfConstraintRuns = 50;

    [Header("Optimizations")]
    [SerializeField] private int _collisionSegmentInterval = 2;

    LineRenderer _lineRenderer;
    private List<RopeLineSegment> _ropeSegments = new List<RopeLineSegment>();
    private Vector3 _ropeStartPoint;

    private void Awake() {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = _numOfRopeSegments;

        _ropeStartPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        for (int i = 0; i < _numOfConstraintRuns; i++) {

            _ropeSegments.Add(new RopeLineSegment(_ropeStartPoint));
            _ropeStartPoint.y -= _ropeSegmentLength;
        }
    }

    private void Update() {
        DrawRope();
    }
    private void FixedUpdate() {
        Simulate();

        for (int i = 0; i < _numOfConstraintRuns; i++) {
            ApplyConstraints();
            if (i % _collisionSegmentInterval == 0) HandleCollisions();
        }
    }

    //绘制
    private void DrawRope() {

        Vector3[] ropePosition = new Vector3[_numOfRopeSegments];

        for (int i = 0; i < _ropeSegments.Count; i++) {
            ropePosition[i] = _ropeSegments[i].CurrentPosition;
        }

        _lineRenderer.SetPositions(ropePosition);
    }

    //重力模拟
    private void Simulate() {
        for (int i = 0; i < _ropeSegments.Count; i++) {
            RopeLineSegment segment = _ropeSegments[i];
            Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * _dampingFactor;

            segment.OldPosition = segment.CurrentPosition;
            segment.CurrentPosition += velocity;
            segment.CurrentPosition += _gravicyForce * Time.fixedDeltaTime;
            _ropeSegments[i] = segment;
        }
    }

    private void ApplyConstraints() {
        RopeLineSegment firstSegment = _ropeSegments[0];
        firstSegment.CurrentPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        _ropeSegments[0] = firstSegment;
        for (int i = 0; i < _numOfRopeSegments - 1; i++) {
            RopeLineSegment currentSeg = _ropeSegments[i];
            RopeLineSegment nextSeg = _ropeSegments[i + 1];

            float dist = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).magnitude;
            float difference = (dist - _ropeSegmentLength);

            Vector2 changeDir = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).normalized;
            Vector2 changeVector = changeDir * difference;

            if (i != 0) {
                currentSeg.CurrentPosition -= (changeVector * 0.5f);
                nextSeg.CurrentPosition += (changeVector * 0.5f);
            } else {
                nextSeg.CurrentPosition += changeVector;
            }
            _ropeSegments[i] = currentSeg;
            _ropeSegments[i + 1] = nextSeg;
        }
    }

    private void HandleCollisions() {
        for (int i = 1; i < _ropeSegments.Count; i++) {
            RopeLineSegment segment = _ropeSegments[i];
            Vector2 velocity = segment.CurrentPosition - segment.OldPosition;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(segment.CurrentPosition, _collisionRadius, _collisionMask);
            if (colliders == null || colliders.Length == 0) continue;
            foreach (Collider2D collider in colliders) {
                Vector2 closestPoint = collider.ClosestPoint(segment.CurrentPosition);
                float distance = Vector2.Distance(segment.CurrentPosition, closestPoint);
                if (distance < _collisionRadius) {
                    Vector2 normal = (segment.CurrentPosition - closestPoint).normalized;
                    if (normal == Vector2.zero) {
                        normal = (segment.CurrentPosition - (Vector2)collider.transform.position).normalized;
                    }
                    float depth = _collisionRadius - distance;
                    segment.CurrentPosition += normal * depth;

                    velocity = Vector2.Reflect(velocity, normal) * _bounceFactor;
                } 
            }
            segment.OldPosition = segment.CurrentPosition = velocity;
            _ropeSegments[i] = segment;
        }
    }

    public struct RopeLineSegment {
        public Vector2 CurrentPosition;

        public Vector2 OldPosition;

        public RopeLineSegment(Vector2 pos) {
            CurrentPosition = pos;
            OldPosition = pos;
        }
    }
}
