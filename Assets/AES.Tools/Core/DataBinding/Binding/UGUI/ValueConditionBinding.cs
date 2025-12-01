using System;
using UnityEngine;
using UnityEngine.Events;
using AES.Tools;

public enum ConditionOperator
{
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual
}

public enum ConditionValueType
{
    Auto,
    Int,
    Float,
    Bool,
    Enum,
    String
}

public class ValueConditionBinding : ContextBindingBase
{
    [Header("Condition Operator")]
    [HideInInspector] public ConditionOperator op = ConditionOperator.Equal;

    [Header("Comparison Type")]
    [HideInInspector] public ConditionValueType valueType = ConditionValueType.Auto;

    [Header("Comparison Values")]
    [HideInInspector] public int intValue;
    [HideInInspector] public float floatValue;
    [HideInInspector] public bool boolValue;
    [HideInInspector] public string stringValue;
    [HideInInspector] public string enumSelectedName;

    [Header("Events (Every change)")]
    [HideInInspector]          public UnityEvent<bool> OnEvaluated;

    [Header("Events (Edge only)")]
    [HideInInspector] public UnityEvent OnBecameTrue;
    [HideInInspector] public UnityEvent OnBecameFalse;
    
    
    IBindingContext _ctx;
    object _token;

    Type _valueActualType;
    string[] _enumNames;
    bool _initialized;

    bool _hasLastResult;
    bool _lastResult;

    protected override void OnContextAvailable(IBindingContext context, string path)
    {
        _ctx = context;
        _token = context.RegisterListener(path, OnModelChanged);
    }

    protected override void OnContextUnavailable()
    {
        if (_ctx != null && _token != null)
            _ctx.RemoveListener(ResolvedPath, OnModelChanged, _token);

        _ctx = null;
        _token = null;

        _valueActualType = null;
        _enumNames = null;
        _initialized = false;

        _hasLastResult = false;
        _lastResult = false;
    }

    void OnModelChanged(object boxed)
    {
        DetectValueType(boxed);
        bool result = Evaluate(boxed);

        // 매번 결과 쏘기
        OnEvaluated?.Invoke(result);

        // Enter/Exit 엣지 감지
        if (!_hasLastResult)
        {
            // 첫 프레임에는 Enter/Exit 쏠지 말지는 취향대로.
            // 여기서는 아무것도 안 쏘고 상태만 저장.
            _hasLastResult = true;
            _lastResult = result;
        }
        else
        {
            if (!_lastResult && result)
                OnBecameTrue?.Invoke();   // false -> true
            else if (_lastResult && !result)
                OnBecameFalse?.Invoke();  // true -> false

            _lastResult = result;
        }

#if UNITY_EDITOR
        Debug_SetLastValue($"{boxed} => {result}");
#endif
    }

    void DetectValueType(object boxed)
    {
        if (boxed == null || _initialized)
            return;

        _valueActualType = boxed.GetType();

        if (valueType == ConditionValueType.Auto)
        {
            if (IsNumeric(_valueActualType)) valueType = ConditionValueType.Float;
            else if (_valueActualType == typeof(bool)) valueType = ConditionValueType.Bool;
            else if (_valueActualType.IsEnum)
            {
                valueType = ConditionValueType.Enum;
                CacheEnum(_valueActualType);
            }
            else if (_valueActualType == typeof(string)) valueType = ConditionValueType.String;
            else valueType = ConditionValueType.String;
        }
        else if (valueType == ConditionValueType.Enum)
        {
            CacheEnum(_valueActualType);
        }

        _initialized = true;
    }

    void CacheEnum(Type t)
    {
        if (t.IsEnum)
            _enumNames = Enum.GetNames(t);
    }

    bool Evaluate(object value)
    {
        if (!_initialized || value == null)
            return false;

        switch (valueType)
        {
            case ConditionValueType.Int:
                return CompareInt(Convert.ToInt32(value), intValue, op);
            case ConditionValueType.Float:
                return CompareFloat(Convert.ToSingle(value), floatValue, op);
            case ConditionValueType.Bool:
                return CompareBool((bool)value, boolValue, op);
            case ConditionValueType.String:
                return CompareString((string)value, stringValue, op);
            case ConditionValueType.Enum:
                return CompareEnumValue(value, enumSelectedName, op);
        }

        return false;
    }

    bool CompareInt(int a, int b, ConditionOperator op)
    {
        return op switch
        {
            ConditionOperator.Equal => a == b,
            ConditionOperator.NotEqual => a != b,
            ConditionOperator.Greater => a > b,
            ConditionOperator.GreaterOrEqual => a >= b,
            ConditionOperator.Less => a < b,
            ConditionOperator.LessOrEqual => a <= b,
            _ => false,
        };
    }

    bool CompareFloat(float a, float b, ConditionOperator op)
    {
        return op switch
        {
            ConditionOperator.Equal => Mathf.Approximately(a, b),
            ConditionOperator.NotEqual => !Mathf.Approximately(a, b),
            ConditionOperator.Greater => a > b,
            ConditionOperator.GreaterOrEqual => a >= b,
            ConditionOperator.Less => a < b,
            ConditionOperator.LessOrEqual => a <= b,
            _ => false,
        };
    }

    bool CompareBool(bool a, bool b, ConditionOperator op)
    {
        return op switch
        {
            ConditionOperator.Equal => a == b,
            ConditionOperator.NotEqual => a != b,
            _ => false,
        };
    }

    bool CompareString(string a, string b, ConditionOperator op)
    {
        return op switch
        {
            ConditionOperator.Equal => a == b,
            ConditionOperator.NotEqual => a != b,
            _ => false,
        };
    }

    bool CompareEnumValue(object value, string selectedName, ConditionOperator op)
    {
        if (_enumNames == null) return false;

        try
        {
            object cmp = Enum.Parse(_valueActualType, selectedName);
            int ai = (int)value;
            int bi = (int)cmp;
            return CompareInt(ai, bi, op);
        }
        catch
        {
            return false;
        }
    }

    bool IsNumeric(Type t)
    {
        return
            t == typeof(int) || t == typeof(float) || t == typeof(double) ||
            t == typeof(long) || t == typeof(short) || t == typeof(uint) ||
            t == typeof(byte) || t == typeof(decimal);
    }
}
