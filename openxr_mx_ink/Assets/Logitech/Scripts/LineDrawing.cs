using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LineDrawing : MonoBehaviour
{
    private List<GameObject> _lines = new List<GameObject>();
    private LineRenderer _currentLine;
    private List<float> _currentLineWidths = new List<float>(); //list to store line widths

    [SerializeField] float _maxLineWidth = 0.01f;
    [SerializeField] float _minLineWidth = 0.0005f;

    [SerializeField] Material _material;

    [SerializeField] private Color _currentColor;
    [SerializeField] private Color highlightColor;
    [SerializeField] private float highlightThreshold = 0.01f;
    private Color _cachedColor;
    private GameObject _highlightedLine;
    private Vector3 _grabStartPosition;
    private Quaternion _grabStartRotation;
    private Vector3[] _originalLinePositions;
    private bool _movingLine = false;
    public Color CurrentColor
    {
        get { return _currentColor; }
        set
        {
            _currentColor = value;
            Debug.Log("LineDrawing color: " + _currentColor.ToString());
        }
    }

    public float MaxLineWidth
    {
        get { return _maxLineWidth; }
        set { _maxLineWidth = value; }
    }

    private bool _lineWidthIsFixed = false;
    public bool LineWidthIsFixed
    {
        get { return _lineWidthIsFixed; }
        set { _lineWidthIsFixed = value; }
    }

    private bool _isDrawing = false;
    private bool _doubleTapDetected = false;

    [SerializeField]
    private float longPressDuration = 1.0f;
    private float buttonPressedTimestamp = 0;

    private StylusHandler _stylusHandler;
    [SerializeField] private DeviceHandler _deviceHandler;
    private Vector3 _previousLinePoint;
    private const float _minDistanceBetweenLinePoints = 0.0005f;

    private void Start()
    {
        _currentColor = Color.black;
        _stylusHandler = _deviceHandler.MxInkStylus;
    }

    private void StartNewLine()
    {
        var gameObject = new GameObject("line");
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        _currentLine = lineRenderer;
        _currentLine.positionCount = 0;
        _currentLine.material = _material;
        _currentLine.material.color = _currentColor;
        _currentLine.loop = false;
        _currentLine.startWidth = _minLineWidth;
        _currentLine.endWidth = _minLineWidth;
        _currentLine.useWorldSpace = true;
        _currentLine.alignment = LineAlignment.View;
        _currentLine.widthCurve = new AnimationCurve();
        _currentLineWidths = new List<float>();
        _currentLine.shadowCastingMode = ShadowCastingMode.Off;
        _currentLine.receiveShadows = false;
        _lines.Add(gameObject);
        _previousLinePoint = new Vector3(0, 0, 0);
    }

    private void AddPoint(Vector3 position, float width)
    {
        if (Vector3.Distance(position, _previousLinePoint) > _minDistanceBetweenLinePoints)
        {
            TriggerHaptics();
            _previousLinePoint = position;
            _currentLine.positionCount++;
            _currentLineWidths.Add(Math.Max(width * _maxLineWidth, _minLineWidth));
            _currentLine.SetPosition(_currentLine.positionCount - 1, position);

            //create a new AnimationCurve
            AnimationCurve curve = new AnimationCurve();

            //populate the curve with keyframes based on the widths list

            for (var i = 0; i < _currentLineWidths.Count; i++)
            {
                curve.AddKey(i / (float)(_currentLineWidths.Count - 1),
                 _currentLineWidths[i]);
            }

            //assign the curve to the widthCurve
            _currentLine.widthCurve = curve;
        }
    }

    private void RemoveLastLine()
    {
        GameObject lastLine = _lines[_lines.Count - 1];
        _lines.RemoveAt(_lines.Count - 1);

        Destroy(lastLine);
    }

    private void ClearAllLines()
    {
        foreach (var line in _lines)
        {
            Destroy(line);
        }
        _lines.Clear();
        _highlightedLine = null;
        _movingLine = false;
    }

    private void TriggerHaptics()
    {
        const float dampingFactor = 0.6f;
        const float duration = 0.01f;
        float middleButtonPressure = _stylusHandler.CurrentState.cluster_middle_value * dampingFactor;
        ((MxInkHandler)_stylusHandler).TriggerHapticPulse(middleButtonPressure, duration);
    }

    void Update()
    {
        _stylusHandler = _deviceHandler.MxInkStylus;

        float analogInput = Mathf.Max(_stylusHandler.CurrentState.tip_value, _stylusHandler.CurrentState.cluster_middle_value);

        if (analogInput > 0 && _stylusHandler.CanDraw())
        {
            if (_highlightedLine)
            {
                UnhighlightLine(_highlightedLine);
                _movingLine = false;
            }

            if (!_isDrawing)
            {
                StartNewLine();
                _isDrawing = true;
            }
            AddPoint(_stylusHandler.CurrentState.inkingPose.position, _lineWidthIsFixed ? 1.0f : analogInput);
            return;
        }
        else
        {
            _isDrawing = false;
        }

        //Undo by double tapping or clicking on cluster_back button on stylus
        if (_stylusHandler.CurrentState.cluster_back_double_tap_value ||
        _stylusHandler.CurrentState.cluster_back_value)
        {
            if (_lines.Count > 0 && !_doubleTapDetected)
            {
                _doubleTapDetected = true;
                buttonPressedTimestamp = Time.time;
                if (_highlightedLine)
                {
                    _lines.Remove(_highlightedLine);
                    Destroy(_highlightedLine);
                    _highlightedLine = null;
                    //haptic click when removing highlighted line
                    ((MxInkHandler)_stylusHandler).TriggerHapticClick();
                    return;
                }
                else
                {
                    RemoveLastLine();
                    //haptic click when deleting last line
                    ((MxInkHandler)_stylusHandler).TriggerHapticClick();
                    return;
                }
            }

            if (_lines.Count > 0 && Time.time >= (buttonPressedTimestamp + longPressDuration))
            {
                //haptic pulse when removing all lines
                ((MxInkHandler)_stylusHandler).TriggerHapticPulse(1.0f, 0.1f);
                ClearAllLines();
                return;
            }
        }
        else
        {
            _doubleTapDetected = false;
        }

        // Look for closest Line
        if (!_movingLine)
        {
            var closestLine = FindClosestLine(_stylusHandler.CurrentState.inkingPose.position);
            if (closestLine)
            {
                if (_highlightedLine != closestLine)
                {
                    if (_highlightedLine)
                    {
                        UnhighlightLine(_highlightedLine);
                    }
                    HighlightLine(closestLine);
                    return;
                }
            }
            else if (_highlightedLine)
            {
                UnhighlightLine(_highlightedLine);
                return;
            }
        }
        if (_stylusHandler.CurrentState.cluster_front_value && !_movingLine)
        {
            _movingLine = true;
            StartGrabbingLine();
        }
        else if (!_stylusHandler.CurrentState.cluster_front_value && _movingLine)
        {
            if (_highlightedLine)
            {
                UnhighlightLine(_highlightedLine);
            }
            _movingLine = false;
        }
        else if (_stylusHandler.CurrentState.cluster_front_value)
        {
            MoveHighlightedLine();
        }
    }

    private GameObject FindClosestLine(Vector3 position)
    {
        GameObject closestLine = null;
        var closestDistance = float.MaxValue;

        foreach (var line in _lines)
        {
            var lineRenderer = line.GetComponent<LineRenderer>();
            for (var i = 0; i < lineRenderer.positionCount - 1; i++)
            {
                var point = FindNearestPointOnLineSegment(lineRenderer.GetPosition(i),
                    lineRenderer.GetPosition(i + 1), position);
                var distance = Vector3.Distance(point, position);

                if (!(distance < closestDistance) || !(distance < highlightThreshold)) continue;
                closestDistance = distance;
                closestLine = line;
            }
        }

        return closestLine;
    }
    private Vector3 FindNearestPointOnLineSegment(Vector3 segStart, Vector3 segEnd, Vector3 point)
    {
        var segVec = segEnd - segStart;
        var segLen = segVec.magnitude;
        var segDir = segVec.normalized;

        var pointVec = point - segStart;
        var projLen = Vector3.Dot(pointVec, segDir);
        var clampedLen = Mathf.Clamp(projLen, 0f, segLen);

        return segStart + segDir * clampedLen;
    }

    private void HighlightLine(GameObject line)
    {
        _highlightedLine = line;
        var lineRenderer = line.GetComponent<LineRenderer>();
        _cachedColor = lineRenderer.material.color;
        lineRenderer.material.color = highlightColor;
        //haptic click when highlighting a line
        ((MxInkHandler)_stylusHandler).TriggerHapticClick();
    }

    private void UnhighlightLine(GameObject line)
    {
        var lineRenderer = line.GetComponent<LineRenderer>();
        lineRenderer.material.color = _cachedColor;
        _highlightedLine = null;
        //haptic click when unhighlighting a line
        ((MxInkHandler)_stylusHandler).TriggerHapticClick();
    }

    private void StartGrabbingLine()
    {
        if (!_highlightedLine) return;
        _grabStartPosition = _stylusHandler.CurrentState.inkingPose.position;
        _grabStartRotation = _stylusHandler.CurrentState.inkingPose.rotation;

        var lineRenderer = _highlightedLine.GetComponent<LineRenderer>();
        _originalLinePositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(_originalLinePositions);
        //haptic pulse when start grabbing a line
        ((MxInkHandler)_stylusHandler).TriggerHapticPulse(1.0f, 0.03f);
    }

    private void MoveHighlightedLine()
    {
        if (!_highlightedLine) return;
        var rotation = _stylusHandler.CurrentState.inkingPose.rotation * Quaternion.Inverse(_grabStartRotation);
        var lineRenderer = _highlightedLine.GetComponent<LineRenderer>();
        var newPositions = new Vector3[_originalLinePositions.Length];

        for (var i = 0; i < _originalLinePositions.Length; i++)
        {
            newPositions[i] = rotation * (_originalLinePositions[i] - _grabStartPosition) + _stylusHandler.CurrentState.inkingPose.position;
        }

        lineRenderer.SetPositions(newPositions);
    }
}
