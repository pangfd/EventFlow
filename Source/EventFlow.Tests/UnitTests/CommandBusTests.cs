﻿// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Logs;
using EventFlow.ReadStores;
using EventFlow.Subscribers;
using EventFlow.Test.Aggregates.Test;
using EventFlow.Test.Aggregates.Test.Commands;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests
{
    [TestFixture]
    public class CommandBusTests
    {
        private CommandBus _sut;
        private Mock<ILog> _logMock;
        private Mock<IEventStore> _eventStoreMock;
        private Mock<IDispatchToEventSubscribers> _dispatchToEventSubscribersMock;
        private Mock<IReadStoreManager> _readStoreManagerMock;
        private EventFlowConfiguration _eventFlowConfiguration;

        [SetUp]
        public void SetUp()
        {
            _logMock = new Mock<ILog>();
            _eventStoreMock = new Mock<IEventStore>();
            _dispatchToEventSubscribersMock = new Mock<IDispatchToEventSubscribers>();
            _readStoreManagerMock = new Mock<IReadStoreManager>();
            _eventFlowConfiguration = new EventFlowConfiguration();

            _sut = new CommandBus(
                _logMock.Object,
                _eventFlowConfiguration,
                _eventStoreMock.Object,
                _dispatchToEventSubscribersMock.Object,
                _readStoreManagerMock.Object);
        }

        [Test]
        public void RetryForOptimisticConcurrencyExceptionsAreDone()
        {
            _eventStoreMock
                .Setup(s => s.LoadAggregateAsync<TestAggregate>(It.IsAny<string>()))
                .Returns(() => Task.FromResult(new TestAggregate("42")));
            _eventStoreMock
                .Setup(s => s.StoreAsync<TestAggregate>(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<IUncommittedDomainEvent>>()))
                .Throws(new OptimisticConcurrencyException(string.Empty, null));

            Assert.Throws<OptimisticConcurrencyException>(async () => await _sut.PublishAsync(new TestACommand("42")).ConfigureAwait(false));

            _eventStoreMock.Verify(
                s => s.StoreAsync<TestAggregate>(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<IUncommittedDomainEvent>>()),
                Times.Exactly(3));
        }
    }
}
