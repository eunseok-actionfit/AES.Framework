##### Maybe
Maybe<int> m1 = Maybe<int>.Some(42);
Maybe<int> m2 = Maybe<int>.None;

##### Result
Result ok = Result.Ok();
Result fail = Result.Fail(new Error("E001", "Invalid input"));

##### Result<T>
var success = Result<int>.Ok(100);
var failure = Result<int>.Fail(new Error("E002", "DB error"));

##### 함수형 스타일
var mapped = success.Map(x => x * 2);  // Ok(200)

var bound  = success.Bind(x => x > 50
? Result<string>.Ok("Big number")
: Result<string>.Fail(new Error("E003", "Too small")));
