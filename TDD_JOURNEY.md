# TDD Journey

## Purpose

이 문서는 C# 서버 학습 과정에서 지금까지 진행한 TDD 흐름을 기록한다.

목표는 단순히 어떤 코드를 만들었는지 나열하는 것이 아니라, **왜 그 테스트를 먼저 만들었고, 테스트가 어떤 설계를 유도했으며, 진행 중 어떤 중복과 리팩토링 지점이 드러났는지**를 남기는 것이다.

## Starting Point

처음 목표는 C#으로 학습용 서버를 만드는 것이었다.

다만 바로 소켓 서버를 만들지 않고, TCP 서버에서 반드시 마주치는 문제를 더 작은 단위로 분리했다.

```text
TCP는 메시지 단위가 아니라 바이트 스트림이다.
따라서 데이터가 잘려서 오거나 여러 패킷이 붙어서 올 수 있다.
```

이 문제를 먼저 테스트 가능한 순수 로직으로 다루기 위해 첫 번째 대상으로 `PacketBuffer`를 선택했다.

패킷 규칙은 다음과 같이 정했다.

```text
[4 bytes: little-endian payload length][payload bytes]
```

예를 들어 payload가 `hello`라면 전체 패킷은 다음과 같다.

```text
05 00 00 00 68 65 6C 6C 6F
```

## Step 1. Header가 부족한 경우

첫 번째 Red는 가장 작은 TCP 조각 수신 상황이었다.

```text
Given: 길이 헤더 4바이트 중 일부만 들어왔다.
When: 패킷을 읽으려고 한다.
Then: 패킷을 반환하지 않는다.
```

테스트 의도:

```text
PacketBuffer는 헤더가 완성되지 않았을 때 패킷을 반환하지 않는다.
```

이 테스트는 `PacketBuffer`라는 타입과 최소 인터페이스를 요구했다.

```csharp
public void Append(byte[] data);
public bool TryReadPacket(out byte[]? packet);
```

처음 Green은 매우 작게 잡았다.

```text
Append는 아무 일도 하지 않는다.
TryReadPacket은 packet = null 후 false를 반환한다.
```

이 단계에서 중요한 점은 아직 실제 패킷 처리 로직을 만들지 않았다는 것이다.

TDD 관점에서 첫 번째 테스트는 “헤더가 부족하면 false”라는 요구사항만 만족시키면 충분했다.

## Step 2. Payload가 부족한 경우

두 번째 Red는 헤더는 완성됐지만 payload가 부족한 상황이었다.

```text
수신 데이터: 05 00 00 00 68 65
```

길이 헤더는 payload 길이 5를 의미하지만 실제 payload는 2바이트만 들어왔다.

요구사항:

```text
PacketBuffer는 payload가 완성되지 않았을 때 패킷을 반환하지 않는다.
```

하지만 이 테스트는 당시 최소 구현에서도 통과할 수 있었다.

이 지점에서 배운 점:

```text
모든 테스트가 항상 새 구현을 강제하는 것은 아니다.
현재 구현을 압박하지 못하는 테스트도 있다.
그럴 때는 더 구체적인 다음 Red가 필요하다.
```

## Step 3. 완성된 단일 패킷 읽기

세 번째 Red는 처음으로 실제 구현을 요구했다.

요구사항:

```text
완성된 패킷 하나가 들어오면 payload를 반환한다.
```

테스트 입력:

```text
05 00 00 00 68 65 6C 6C 6F
```

기대 결과:

```text
true
68 65 6C 6C 6F
```

이 테스트를 통과하기 위해 `PacketBuffer`에는 다음 책임이 생겼다.

```text
1. Append로 받은 바이트를 내부 버퍼에 누적한다.
2. 내부 버퍼가 4바이트 미만이면 false를 반환한다.
3. 앞 4바이트를 payload length로 읽는다.
4. 전체 payload가 아직 부족하면 false를 반환한다.
5. payload가 완성됐으면 복사해서 반환한다.
6. 읽은 패킷은 내부 버퍼에서 제거한다.
```

이 시점에서 `List<byte>` 기반 내부 버퍼가 도입되었다.

## Step 4. 여러 패킷이 붙어서 들어오는 경우

다음 테스트는 TCP에서 자주 발생하는 상황을 고정하기 위한 것이었다.

```text
[hello 패킷][world 패킷]
```

요구사항:

```text
PacketBuffer는 완성된 패킷 두 개가 한 번에 들어오면 순서대로 읽을 수 있다.
```

이 테스트는 현재 구현에서 통과할 가능성이 높았다.

이유는 첫 번째 패킷을 읽은 뒤 다음 코드가 이미 실행되고 있었기 때문이다.

```csharp
buffer.RemoveRange(0, HeaderSize + payloadLength);
```

배운 점:

```text
테스트는 항상 새 코드를 강제해야만 의미가 있는 것은 아니다.
중요한 프로토콜 규칙을 고정해서 이후 리팩토링을 안전하게 만드는 역할도 한다.
```

## Step 5. 완성된 패킷과 다음 패킷 일부가 같이 들어오는 경우

다음 테스트는 다음과 같은 수신 상태를 검증했다.

```text
[05 00 00 00 hello][05 00]
```

첫 번째 패킷은 완성됐지만, 두 번째 패킷의 헤더는 2바이트만 들어온 상태다.

요구사항:

```text
완성된 첫 번째 패킷만 반환하고,
불완전한 다음 패킷 일부는 내부 버퍼에 유지한다.
```

기대 흐름:

```text
첫 번째 TryReadPacket -> true, hello
두 번째 TryReadPacket -> false, null
```

이 테스트 역시 TCP 스트림 처리에서 매우 중요한 정상 흐름을 고정했다.

## Step 6. 남은 조각에 추가 데이터가 붙는 경우

이전 테스트는 불완전한 데이터가 남아 있어야 한다는 것을 간접적으로 확인했다.

다음 테스트는 그 동작을 더 강하게 검증했다.

첫 번째 수신:

```text
[05 00 00 00 hello][05 00]
```

두 번째 수신:

```text
[00 00 world]
```

두 번째 패킷은 합쳐지면 다음과 같이 완성된다.

```text
[05 00 00 00 world]
```

요구사항:

```text
PacketBuffer는 불완전한 패킷 일부를 내부에 보관하다가,
나머지 데이터가 추가되면 완성된 패킷으로 읽을 수 있다.
```

이 테스트를 통해 “불완전해서 false를 반환하더라도 받은 데이터를 버리면 안 된다”는 규칙을 명확히 했다.

## Step 7. 음수 payload length 방어

정상 흐름을 어느 정도 고정한 뒤, 비정상 입력 방어로 넘어갔다.

문제 상황:

```text
FF FF FF FF
```

little-endian `int32`로 해석하면 `-1`이다.

초기 구현은 길이 검증 전에 다음 계산을 먼저 할 수 있었다.

```csharp
HeaderSize + payloadLength
```

이 경우 payload length가 음수이면 논리적으로 잘못된 상태가 된다.

요구사항:

```text
payload length가 음수이면 InvalidOperationException을 던진다.
```

여기서 `false`를 반환하지 않기로 했다.

이유:

```text
false는 "아직 데이터가 부족하다"는 뜻이다.
음수 길이는 데이터를 더 받는다고 해결되는 상태가 아니라 프로토콜 위반이다.
```

따라서 의미를 다음처럼 나누었다.

```text
false: 정상 상황이지만 아직 패킷이 완성되지 않음
true: 패킷 하나를 성공적으로 읽음
InvalidOperationException: 프로토콜 위반
```

이 과정에서 음수 길이 검사는 payload length를 읽은 직후에 수행하도록 위치를 바로잡았다.

## Step 8. 최대 payload length 방어

다음 방어 테스트는 너무 큰 payload length를 거부하는 것이었다.

요구사항:

```text
payload length가 허용 최대 크기보다 크면 InvalidOperationException을 던진다.
```

이를 위해 `PacketBuffer`는 생성자로 최대 payload 길이를 받을 수 있게 되었다.

```csharp
public PacketBuffer(int maxPayloadLength = DefaultMaxPayloadLength)
```

테스트에서는 제한을 작게 설정했다.

```text
maxPayloadLength: 4
payloadLength: 5
```

배운 점:

```text
데이터 부족은 기다리면 해결된다.
하지만 최대 길이 초과는 더 기다리면 안 된다.
따라서 false가 아니라 예외가 맞다.
```

이 단계는 서버 안정성과 보안 관점에서 중요하다.

잘못된 길이 값을 그대로 믿으면 메모리 과다 사용, 예외, 연결 장애로 이어질 수 있다.

## Step 9. Refactor

기능이 어느 정도 갖춰진 뒤, 테스트가 통과하는 상태에서 리팩토링을 진행했다.

리팩토링 후보:

```csharp
var payloadLength = BitConverter.ToInt32(buffer.GetRange(0, HeaderSize).ToArray(), 0);
```

이 코드는 동작은 맞지만 `TryReadPacket` 안에서 세부 구현이 드러나 있었다.

이를 다음 private 메서드로 분리했다.

```csharp
private int ReadPayloadLength()
```

이 리팩토링의 목적은 성능 최적화가 아니라 의도 분리였다.

배운 점:

```text
테스트는 기능 추가뿐 아니라 리팩토링을 가능하게 한다.
테스트가 통과하는 한, 내부 구조를 더 읽기 좋게 바꿀 수 있다.
```

## Step 10. PacketEncoder 도입

`PacketBufferTest`를 작성하다 보니 패킷 바이트를 매번 손으로 만들고 있었다.

예:

```text
05 00 00 00 68 65 6C 6C 6F
```

이 중복은 테스트 가독성을 떨어뜨렸다.

그래서 다음 대상으로 `PacketEncoder`를 선택했다.

요구사항:

```text
PacketEncoder는 payload를 받아서
[4바이트 little-endian length][payload] 형태의 byte[]를 만든다.
```

첫 Red:

```text
payload가 hello이면 length-prefixed packet을 반환한다.
```

다음 테스트:

```text
빈 payload는 길이 0의 header-only packet으로 인코딩된다.
```

이를 통해 `PacketEncoder.Encode(byte[] payload)`가 만들어졌다.

## Step 11. 테스트 중복 발견

`PacketEncoderTest`를 만든 뒤 확인해 보니, `PacketBufferTest` 아래에도 encoder 성격의 테스트가 들어가 있었다.

중복된 성격:

```text
Encode_ReturnsLengthPrefixedPacket_WhenPayloadIsGiven
```

문제점:

```text
PacketBufferTest는 PacketBuffer의 책임을 검증해야 한다.
PacketEncoder의 인코딩 규칙은 PacketEncoderTest에서 검증하는 것이 맞다.
```

따라서 `PacketBufferTest`에 남아 있는 encoder 테스트는 제거하는 것이 좋다.

이건 기능 변경이 아니라 테스트 책임 분리다.

## Current State

현재까지 만들어진 핵심 요소는 다음과 같다.

```text
PacketBuffer
- 수신 byte[] 누적
- 헤더 부족 시 false
- payload 부족 시 false
- 완성된 패킷 payload 반환
- 여러 패킷 순차 반환
- 불완전한 다음 패킷 보존
- 음수 payload length 거부
- 최대 payload length 초과 거부

PacketEncoder
- payload를 length-prefixed packet으로 인코딩
- 빈 payload 인코딩
```

테스트를 통해 고정한 핵심 규칙은 다음과 같다.

```text
1. TCP 수신 데이터는 조각날 수 있다.
2. TCP 수신 데이터는 여러 패킷이 붙어서 올 수 있다.
3. 불완전한 데이터는 버리면 안 된다.
4. false는 "아직 부족함"을 의미한다.
5. 프로토콜 위반은 예외로 구분한다.
6. 테스트는 리팩토링의 안전망이다.
7. 테스트에도 책임 분리와 중복 제거가 필요하다.
```

## Next Recommended Step

다음으로는 `PacketEncoder`와 `PacketBuffer`가 같은 wire format을 사용하는지 확인하는 작은 통합 테스트를 추가하는 것이 좋다.

예상 테스트:

```text
PacketEncoder로 payload를 인코딩한다.
인코딩된 packet을 PacketBuffer에 Append한다.
TryReadPacket으로 원래 payload가 반환되는지 확인한다.
```

그 다음 큰 주제는 `Session`이다.

다만 바로 소켓을 열지 않고, 먼저 다음 요구사항을 테스트 가능한 순수 로직으로 정의한다.

```text
Session은 수신된 byte[]를 PacketBuffer에 넣고,
완성된 payload가 있으면 컨텐츠 계층으로 전달한다.
```

이렇게 하면 네트워크 없이도 수신 처리 흐름을 테스트할 수 있다.

