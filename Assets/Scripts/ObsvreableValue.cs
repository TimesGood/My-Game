using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//观察者模式监听属性
public class ObsvreableValue<T>
{
    [SerializeField] protected T value;
    public delegate void ValueChanged(T oldValue, T newValue);
    public event ValueChanged OnValueChanged;
    //public void SetValueWithoutNotify(T newValue) {
    //    value = newValue;
    //}

    public T Value {
        get {
            return value;
        }
        set {
            var old = this.value;
            if (old.Equals(value)) return;
            this.value = value;
            OnValueChanged?.Invoke(old, this.value);
        }
    }
}
