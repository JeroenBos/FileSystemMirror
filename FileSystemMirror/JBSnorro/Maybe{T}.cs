using System.Diagnostics.CodeAnalysis;

namespace JBSnorro
{
	public struct Maybe<T>
	{
		public static Maybe<T> None { get; } = new Maybe<T>();
		public T? Value { get; }
		[MemberNotNullWhen(true, nameof(Value))]
		public bool HasValue { get; }

		public Maybe(T value)
		{
			this.HasValue = true;
			this.Value = value;
		}

		public static implicit operator Maybe<T>(T value) => new(value);
	}
}