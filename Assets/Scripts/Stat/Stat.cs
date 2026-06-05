using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//数值对象
[System.Serializable]
public class Stat : ObsvreableValue<int>
{ 
    public List<int> modifiers;//数值修饰。可能存在各种增益/负面效果的情况

    //获取数值
    public int GetValue()
    {
        int finalValue = this.value;
        foreach (int modifier in modifiers)
        {
            finalValue += modifier;
        }
        return finalValue;
    }

    //设置默认值
    public void SetDefaultValue(int value)
    {
        this.value = value;
    }

    //添加属性修饰
    public void AddModifier(int modifier)
    {
        this.modifiers.Add(modifier);
    }

    //删除属性修饰
    public void RemoveModifier(int modifier)
    {
        this.modifiers.Remove(modifier);
    }
    
}
