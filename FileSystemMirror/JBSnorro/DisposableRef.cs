using System;

namespace JBSnorro
{
	class DisposableRef : IDisposable
	{
		internal IDisposable? Current { get; private set; }
		public bool IsEmpty => this.Current is null;
		public void Dispose()
		{
			Current?.Dispose();
		}
		public void SetDisposable(IDisposable? d)
		{
			if (d != null && this.Current != null)
				throw new Exception("Must first clear out current disposable");

			this.Current = d;
		}

		public override bool Equals(object? obj) => throw new NotSupportedException();
		public override int GetHashCode() => throw new NotSupportedException();
	}

	class BinaryDisposableRef : IDisposable
	{
		internal IDisposable? Primary { get; private set; }
		internal IDisposable? Secondary { get; private set; }
		public void Dispose()
		{
			Primary?.Dispose();
			Secondary?.Dispose();
		}
		public bool IsEmpty => Primary is null && Secondary is null;
		public void SetPrimary(IDisposable? d)
		{
			if (d != null && Primary != null)
				throw new Exception("Must first clear out current disposable");

			this.Primary = d;
		}
		public void SetSecondary(IDisposable? d)
		{
			if (d != null && this.Secondary != null)
				throw new Exception("Must first clear out current disposable");

			this.Secondary = d;
		}

		public override bool Equals(object? obj) => throw new NotSupportedException();
		public override int GetHashCode() => throw new NotSupportedException();
	}
}