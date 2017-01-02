﻿namespace Gu.Wpf.Reactive
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;

    using Gu.Reactive;

    /// <summary>
    /// Exposes AdornedElement and sets DataContext to the CommandProxy of the adorned element
    /// </summary>
    public class ConditionToolTip : ToolTip
    {
        public static readonly DependencyProperty ConditionProperty = DependencyProperty.Register(
            "Condition",
            typeof(ICondition),
            typeof(ConditionToolTip),
            new PropertyMetadata(default(ICondition)));

        public static readonly DependencyProperty InferConditionFromCommandProperty = DependencyProperty.Register(
            "InferConditionFromCommand",
            typeof(bool),
            typeof(ConditionToolTip),
            new PropertyMetadata(true, OnInferConditionFromCommandChanged));

        private static readonly DependencyPropertyKey CommandTypePropertyKey = DependencyProperty.RegisterReadOnly(
            "CommandType",
            typeof(Type),
            typeof(ConditionToolTip),
            new PropertyMetadata(default(Type)));

        public static readonly DependencyProperty CommandTypeProperty = CommandTypePropertyKey.DependencyProperty;

        private static readonly DependencyProperty PlacementTargetProxyProperty = DependencyProperty.Register(
            "PlacementTargetProxy",
            typeof(UIElement),
            typeof(ConditionToolTip),
            new PropertyMetadata(
                default(UIElement),
                OnPlacementTargetProxyChanged));

        private static readonly DependencyProperty CommandProxyProperty = DependencyProperty.Register(
            "CommandProxy",
            typeof(ICommand),
            typeof(ConditionToolTip),
            new PropertyMetadata(default(ICommand), OnCommandProxyChanged));

        static ConditionToolTip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConditionToolTip), new FrameworkPropertyMetadata(typeof(ConditionToolTip)));
        }

        public ConditionToolTip()
        {
            this.UpdateInferConditionFromCommand(this.InferConditionFromCommand);
        }

        public bool InferConditionFromCommand
        {
            get { return (bool)this.GetValue(InferConditionFromCommandProperty); }
            set { this.SetValue(InferConditionFromCommandProperty, value); }
        }

        /// <summary>
        /// The condition if the command is a ConditionRelayCommand null otherwise
        /// </summary>
        public ICondition Condition
        {
            get { return (ICondition)this.GetValue(ConditionProperty); }
            set { this.SetValue(ConditionProperty, value); }
        }

        public Type CommandType
        {
            get { return (Type)this.GetValue(CommandTypeProperty); }
            protected set { this.SetValue(CommandTypePropertyKey, value); }
        }

        private static void OnPlacementTargetProxyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var commandToolTip = (ConditionToolTip)o;
            var target = commandToolTip.PlacementTarget as ButtonBase;
            if (target == null)
            {
                commandToolTip.SetCurrentValue(CommandProxyProperty, null);
            }
            else
            {
                var command = target.GetValue(ButtonBase.CommandProperty) as IConditionRelayCommand;
                commandToolTip.SetCurrentValue(CommandProxyProperty, command);
            }
        }

        private static void OnCommandProxyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var commandToolTip = (ConditionToolTip)o;
            var command = e.NewValue as IConditionRelayCommand;
            if (command == null)
            {
                commandToolTip.SetCurrentValue(ConditionProperty, null);
                commandToolTip.CommandType = null;
                return;
            }

            commandToolTip.SetCurrentValue(ConditionProperty, command.Condition);
            commandToolTip.CommandType = command.GetType();
        }

        private static void OnInferConditionFromCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var conditionToolTip = (ConditionToolTip)d;
            conditionToolTip.UpdateInferConditionFromCommand((bool)e.NewValue);
        }

        private void UpdateInferConditionFromCommand(bool infer)
        {
            if (infer)
            {
                BindingOperations.SetBinding(
                    this,
                    PlacementTargetProxyProperty,
                    this.CreateOneWayBinding(PlacementTargetProperty));
            }
            else
            {
                BindingOperations.ClearBinding(this, PlacementTargetProxyProperty);
            }
        }
    }
}
