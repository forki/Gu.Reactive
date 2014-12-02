﻿namespace Gu.Wpf.Reactive.Tests
{
    using System;
    using System.ComponentModel;
    using System.Reactive;

    using Gu.Reactive;
    using Gu.Reactive.Tests;

    using NUnit.Framework;

    public class ConditionParameterRelayCommandTests
    {
        private FakeInpc _fake;
        private Condition _condition;
        private ConditionRelayCommand<int> _command;
        private IObservable<EventPattern<PropertyChangedEventArgs>> _observable;

        [SetUp]
        public void SetUp()
        {
            _fake = new FakeInpc { Prop1 = false };
            _observable = _fake.ToObservable(x => x.Prop1);
            _condition = new Condition(_observable, () => _fake.Prop1);
            _command = new ConditionRelayCommand<int>(x => { }, _condition);
        }
        [Test]
        public void NotifiesOnConditionChanged()
        {
            int count = 0;
            _command.CanExecuteChanged += (sender, args) => count++;
            _fake.Prop1 = true;
            Assert.AreEqual(1, count);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CanExecute(bool expected)
        {
            _fake.Prop1 = expected;
            Assert.AreEqual(expected, _command.CanExecute(0));
        }

        [Test]
        public void Execute()
        {
            var i = 0;
            var command = new ConditionRelayCommand<int>(x => i=x, _condition, false);
            command.Execute(1);
            Assert.AreEqual(1, i);
        }
    }
}