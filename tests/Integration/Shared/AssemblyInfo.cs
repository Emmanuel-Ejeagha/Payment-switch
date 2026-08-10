using Xunit;

// Integration tests spin up Testcontainers (Postgres, RabbitMQ) per factory
// class. Running classes in parallel multiplies Docker stacks and causes
// flaky connection failures, so integration projects run their classes
// sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
