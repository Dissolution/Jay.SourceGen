###### Implementable Interfaces
- `IEquatable<TSelf>`
  - requires [Equatable]/[Key]/??? Attribute or defaults to Record?
  - also implements `==` and `!=`
  - effects `GetHashCode()` and `Equals(object?)`
- `IComparable<TSelf>`
  - requires [Comparable]/[Key]/??? Attribute or defaults to Record?
  - also implements `>`, `<`, `>=`, `<=`
- `IDisposable`
  - requires [Dispose] attribute on Field(s), Property(s), and/or Event(s)
- `INotifyPropertyChanged` + `INotifyPropertyChanging`
  - auto-implemented for all Public Properties
  - both are implemented if either are
- `IFormattable`
  - Supports custom formatting?
- `ICloneable`
  - 

##### Methods
- Most of the above will effect the output of `ToString()`
- Key members will be formatted in the output (as per `record`)
- Can be effected by [Display]?

