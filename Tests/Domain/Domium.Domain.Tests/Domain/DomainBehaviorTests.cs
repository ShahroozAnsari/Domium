using Domium.Domain;
using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.Events;
using Domium.Domain.Abstractions.Entity;
using Domium.Domain.Abstractions.ValueObject;
using Domium.Eventing.Abstractions;

namespace Domium.Tests.Domain;

public sealed class DomainBehaviorTests
{
    [Fact]
    public void AggregateId_EmptyGuid_RejectsMissingIdentity()
    {
        // Arrange
        var value = Guid.Empty;
        // Act
        var error = Assert.Throws<ArgumentException>(() => new TestId(value));
        // Assert
        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void AggregateId_NullReferenceValue_RejectsMissingIdentity()
    {
        // Arrange
        string value = null!;
        // Act
        var error = Assert.Throws<ArgumentNullException>(() => new TextId(value));
        // Assert
        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void AggregateId_EqualValues_WorkAsDictionaryKeys()
    {
        // Arrange
        var value = Guid.NewGuid();
        var original = new TestId(value);
        var sameIdentity = new TestId(value);
        var dictionary = new Dictionary<TestId, string> { [original] = "aggregate" };
        // Act
        var found = dictionary[sameIdentity];
        // Assert
        Assert.Equal("aggregate", found);
        Assert.Equal(value.ToString(), original.ToString());
        Assert.IsAssignableFrom<IAggregateId<Guid>>(original);
        Assert.NotEqual<ValueObject>(original, new OtherId(value));
    }

    [Fact]
    public void ValueObject_NullAndOrderedComponents_UseValueEqualityAndHashing()
    {
        // Arrange
        ValueObject first = new Components("EUR", null, 12m);
        ValueObject equal = new Components("EUR", null, 12m);
        ValueObject reordered = new Components(null, "EUR", 12m);
        var values = new HashSet<ValueObject> { first };
        // Act
        var duplicateAdded = values.Add(equal);
        // Assert
        Assert.False(duplicateAdded);
        Assert.True(first == equal);
        Assert.False(first != equal);
        Assert.True(first != reordered);
        Assert.False(first.Equals(null));
        Assert.False(first.Equals("EUR"));
        Assert.True(first.Equals(first));
        Assert.IsAssignableFrom<IValueObject>(first);
    }

    [Fact]
    public void ValueObject_NullOperators_AreSymmetric()
    {
        // Arrange
        ValueObject? missing = null;
        ValueObject? alsoMissing = null;
        ValueObject value = new Components("EUR");
        // Act
        var bothMissingEqual = missing == alsoMissing;
        // Assert
        Assert.True(bothMissingEqual);
        Assert.True(value != missing);
        Assert.True(missing != value);
    }

    [Fact]
    public void Entity_NullIdentifier_RejectsConstruction()
    {
        // Arrange
        TestId identifier = null!;
        // Act
        var error = Assert.Throws<ArgumentNullException>(() => new TestEntity(identifier));
        // Assert
        Assert.Equal("id", error.ParamName);
    }

    [Fact]
    public void Entity_EqualConcreteTypeAndId_WorkInHashSetsAndOperators()
    {
        // Arrange
        var id = Guid.NewGuid();
        EntityBase<TestId> entity = new TestEntity(new TestId(id));
        EntityBase<TestId> equal = new TestEntity(new TestId(id));
        EntityBase<TestId> otherType = new OtherEntity(new TestId(id));
        var entities = new HashSet<EntityBase<TestId>> { entity };
        // Act
        var duplicateAdded = entities.Add(equal);
        // Assert
        Assert.False(duplicateAdded);
        Assert.True(entity == equal);
        Assert.False(entity != equal);
        Assert.True(entity != otherType);
        Assert.False(entity.Equals(null));
        Assert.False(entity.Equals("entity"));
        Assert.True(entity.Equals(entity));
        Assert.IsAssignableFrom<IEntityBase<TestId>>(entity);
    }

    [Fact]
    public void Entity_NullOperators_AreSymmetric()
    {
        // Arrange
        EntityBase<TestId>? missing = null;
        EntityBase<TestId>? alsoMissing = null;
        EntityBase<TestId> entity = new TestEntity(new TestId(Guid.NewGuid()));
        // Act
        var bothMissingEqual = missing == alsoMissing;
        // Assert
        Assert.True(bothMissingEqual);
        Assert.True(entity != missing);
        Assert.True(missing != entity);
    }

    [Fact]
    public void DomainEvent_ExplicitIdentityAndTime_PreserveTheOriginalFact()
    {
        // Arrange
        var id = Guid.NewGuid();
        var occurredOn = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
        // Act
        var fact = new TestEvent(id, occurredOn);
        // Assert
        Assert.Equal(id, fact.EventId);
        Assert.Equal(occurredOn, fact.OccurredOn);
        Assert.IsAssignableFrom<IDomainEvent>(fact);
    }

    [Fact]
    public void DomainEvent_DefaultConstructor_GeneratesUniqueFactIdentities()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        // Act
        var first = new TestEvent();
        var second = new TestEvent();
        // Assert
        Assert.NotEqual(Guid.Empty, first.EventId);
        Assert.NotEqual(first.EventId, second.EventId);
        Assert.InRange(first.OccurredOn, before, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void DomainExceptions_PreserveTheMessageAndOriginalCause()
    {
        // Arrange
        var cause = new InvalidOperationException("storage conflict");
        // Act
        var domain = new DomainException("rule rejected", cause);
        var concurrency = new DomiumConcurrencyException("revision changed", cause);
        var notFound = new DomainNotFoundException("aggregate missing");
        // Assert
        Assert.Same(cause, domain.InnerException);
        Assert.Equal("rule rejected", domain.Message);
        Assert.Same(cause, concurrency.InnerException);
        Assert.Equal("revision changed", concurrency.Message);
        Assert.IsAssignableFrom<DomainException>(notFound);
        Assert.Equal("aggregate missing", notFound.Message);
        Assert.Equal("revision changed", new DomiumConcurrencyException("revision changed").Message);
        Assert.Equal("rule rejected", new DomainException("rule rejected").Message);
    }

    [Fact]
    public void Aggregate_NullEventBus_RejectsConstruction()
    {
        // Arrange
        var id = new TestId(Guid.NewGuid());
        // Act
        var error = Assert.Throws<ArgumentNullException>(() => new TestAggregate(id, null!));
        // Assert
        Assert.Equal("eventBus", error.ParamName);
    }

    [Fact]
    public async Task RaiseEvent_WithoutAnAttachedBus_ExplainsTheRequiredCreationPath()
    {
        // Arrange
        var aggregate = new TestAggregate(new TestId(Guid.NewGuid()));
        // Act
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => aggregate.Publish(new TestEvent()));
        // Assert
        Assert.Contains("No event bus", error.Message);
    }

    [Fact]
    public async Task RaiseEvent_NullEvent_IsRejectedBeforeDispatch()
    {
        // Arrange
        var bus = new RecordingBus();
        var aggregate = new TestAggregate(new TestId(Guid.NewGuid()), bus);
        // Act
        var error = await Assert.ThrowsAsync<ArgumentNullException>(() => aggregate.Publish(null!));
        // Assert
        Assert.Equal("event", error.ParamName);
        Assert.Empty(bus.Events);
    }

    [Fact]
    public async Task CreateAsync_NullAggregate_RejectsMissingAggregate()
    {
        // Arrange
        var fact = new TestEvent();
        // Act
        var error = await Assert.ThrowsAsync<ArgumentNullException>(() => TestAggregate.CreateForTest(null!, fact));
        // Assert
        Assert.Equal("aggregate", error.ParamName);
    }

    [Fact]
    public async Task CreateAsync_PendingDispatch_DoesNotReturnBeforeTheEventCompletes()
    {
        // Arrange
        var bus = new RecordingBus { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var aggregate = new TestAggregate(new TestId(Guid.NewGuid()), bus);
        var fact = new TestEvent();
        // Act
        var creation = TestAggregate.CreateForTest(aggregate, fact);
        var returnedBeforeDispatch = creation.IsCompleted;
        bus.Gate.SetResult(true);
        var created = await creation;
        // Assert
        Assert.False(returnedBeforeDispatch);
        Assert.Same(aggregate, created);
        Assert.Same(fact, Assert.Single(bus.Events));
        Assert.IsAssignableFrom<IAggregateRoot<TestId>>(created);
    }

    [Fact]
    public async Task RaiseEvent_FailedDispatch_PropagatesTheOriginalFailure()
    {
        // Arrange
        var cause = new InvalidOperationException("subscriber failed");
        var bus = new RecordingBus { Failure = cause };
        var aggregate = new TestAggregate(new TestId(Guid.NewGuid()), bus);
        // Act
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => aggregate.Publish(new TestEvent()));
        // Assert
        Assert.Same(cause, error);
    }

    private sealed class TestId(Guid value) : AggregateId<Guid>(value);
    private sealed class OtherId(Guid value) : AggregateId<Guid>(value);
    private sealed class TextId(string value) : AggregateId<string>(value);
    private sealed class TestEntity(TestId id) : EntityBase<TestId>(id);
    private sealed class OtherEntity(TestId id) : EntityBase<TestId>(id);
    private sealed class Components(params object?[] values) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents() => values;
    }

    private sealed class TestEvent : DomainEvent
    {
        public TestEvent() { }
        public TestEvent(Guid id, DateTimeOffset occurredOn) : base(id, occurredOn) { }
    }

    private sealed class TestAggregate : AggregateRoot<TestId>
    {
        public TestAggregate(TestId id) : base(id) { }
        public TestAggregate(TestId id, IEventBus bus) : base(id, bus) { }
        public Task Publish(IDomiumEvent fact) => RaiseEvent(fact);
        public static Task<TestAggregate> CreateForTest(TestAggregate aggregate, IDomiumEvent fact) =>
            CreateAsync(aggregate, fact);
    }

    private sealed class RecordingBus : IEventBus
    {
        public List<IDomiumEvent> Events { get; } = [];
        public TaskCompletionSource<bool>? Gate { get; init; }
        public Exception? Failure { get; init; }
        public Task PublishAsync<TEvent>(TEvent fact, CancellationToken cancellationToken = default)
            where TEvent : IDomiumEvent
        {
            if (Failure is not null) return Task.FromException(Failure);
            Events.Add(fact);
            return Gate?.Task ?? Task.CompletedTask;
        }
        public async Task PublishAsync(IReadOnlyCollection<IDomiumEvent> facts, CancellationToken cancellationToken = default)
        {
            foreach (var fact in facts) await PublishAsync(fact, cancellationToken);
        }
    }
}
