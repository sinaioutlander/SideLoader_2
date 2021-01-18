﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SideLoader.UI.Inspectors.Reflection
{
    public class TypeTreeDropdown
    {
        public event Action<Type> OnValueSelected;

        internal readonly Type m_baseType;
        internal readonly List<Type> m_typeOptions;

        internal GameObject m_uiContent;
        internal Dropdown m_dropdown;

        public TypeTreeDropdown(Type type, GameObject parent, Type currentType, Action<Type> listener)
        {
            m_baseType = type;
            m_typeOptions = At.GetChangeableTypes(m_baseType);

            OnValueSelected += listener;

            ConstructUI(parent, currentType);
        }

        internal void InvokeOnValueSelected(int value)
        {
            var type = m_typeOptions[value];
            OnValueSelected.Invoke(type);
        }

        private void ConstructUI(GameObject parent, Type currentType)
        {
            m_uiContent = UIFactory.CreateDropdown(parent, out m_dropdown);

            var dropLayout = m_uiContent.AddComponent<LayoutElement>();
            dropLayout.minHeight = 25;
            dropLayout.minWidth = 250;
            dropLayout.flexibleHeight = 0;
            dropLayout.flexibleWidth = 0;

            m_dropdown.options.AddRange(m_typeOptions.Select(it => new Dropdown.OptionData { text = it.Name }));

            m_dropdown.value = m_typeOptions.IndexOf(currentType);

            m_dropdown.onValueChanged.AddListener(InvokeOnValueSelected);
        }
    }
}
