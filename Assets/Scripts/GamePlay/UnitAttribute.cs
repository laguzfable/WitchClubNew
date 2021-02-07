using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Kenaz
{
    [Serializable]
    public class UnitAttribute
    {
        protected float curValue;

        [SerializeField]
        protected float baseValue;
        [HideInInspector]
        public float buff;
        [HideInInspector]
        public float equipmentValue;
        [HideInInspector]
        public float modifier = 1f;

        public bool canLessZero = false;

        public event Action<float> OnValueChanged;
        public event Action OnValueFull;
        public event Action OnValueEmpty;

        public UnitAttribute()
        {

        }

        public UnitAttribute (float newValue) : this(newValue, true)
        {
            
        }

        public UnitAttribute(float newValue, bool isRestore)
        {
            SetBaseValue(newValue);
            if(isRestore)
            {
                Restore();
            }
        }

        public void SetBaseValue(float newValue)
        {
            baseValue = newValue;
        }

        public float GetTotalValue()
        {
            return (baseValue + buff + equipmentValue) * modifier;
        }

        /// <summary>
        /// Get total value without modifier.
        /// </summary>
        /// <returns></returns>
        public float GetTotalValuePure()
        {
            return (baseValue + buff + equipmentValue);
        }

        public float Value
        {
            set
            {
                curValue = value;
                if (curValue > GetTotalValue())
                {
                    curValue = GetTotalValue();
                    OnValueFull?.Invoke();
                }
                else if (curValue <= 0f)
                {
                    if (!canLessZero)
                    {
                        curValue = 0f;
                    }
                    OnValueEmpty?.Invoke();
                }
                OnValueChanged?.Invoke(value);
            }
            get
            {
                return curValue;
            }
        }

        public void ResetBuffAndModify()
        {
            buff = 0f;
            modifier = 1f;
        }

        public void Restore()
        {
            Value = GetTotalValue();
        }

        public float GetPercent()
        {
            return curValue / GetTotalValue();
        }

        static public UnitAttribute operator +(UnitAttribute attr, float value)
        {
            attr.Value += value;
            return attr;
        }

        static public UnitAttribute operator -(UnitAttribute attr, float value)
        {
            attr.Value -= value;
            return attr;
        }
    }
}