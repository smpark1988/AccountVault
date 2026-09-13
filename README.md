# AccountVault

AccountVault는 사이트별 계정 정보를 로컬에 암호화해서 저장하는 가벼운 WPF 데스크톱 앱입니다.

외부 서버, 클라우드, 로그인 API를 사용하지 않고, 실행 파일과 같은 경로의 `data/vault.dat` 파일에 모든 데이터를 암호화해 보관합니다.

## 주요 기능

- 마스터 비밀번호 기반 보관함 생성 및 로그인
- 사이트별 계정 추가, 수정, 삭제
- 사이트명, URL, 아이디 기준 검색
- 비밀번호 기본 숨김 표시
- 비밀번호 보기/숨기기 전환
- 아이디 및 비밀번호 클립보드 복사
- 비밀번호 복사 후 30초 뒤 조건부 클립보드 자동 삭제
- 5분 미사용 시 자동 잠금
- `vault.dat` 백업 내보내기 및 가져오기
- Fluent UI 느낌의 단순한 WPF 인터페이스

## 보안 설계

AccountVault는 마스터 비밀번호 원문을 파일에 저장하지 않습니다.

계정 데이터는 JSON으로 직렬화한 뒤 AES-GCM으로 암호화하여 저장합니다.

암호화 키는 다음 방식으로 생성합니다.

- KDF: PBKDF2-SHA256
- 반복 횟수: 300,000회
- salt: 32바이트
- key: 32바이트
- AES-GCM nonce: 12바이트
- AES-GCM tag: 16바이트

`vault.dat`에는 암호문과 복호화에 필요한 메타데이터만 저장됩니다.

```json
{
  "version": 1,
  "kdf": "PBKDF2-SHA256",
  "iterations": 300000,
  "salt": "base64",
  "nonce": "base64",
  "tag": "base64",
  "cipherText": "base64"
}
```

복호화에 실패하면 다음 메시지를 표시합니다.

```text
마스터 비밀번호가 올바르지 않거나 데이터 파일이 손상되었습니다.
```

## 저장 위치

데이터 파일은 실행 파일 기준으로 다음 위치에 생성됩니다.

```text
AccountVault.exe
data/vault.dat
```

개발 중 `dotnet run`으로 실행하면 보통 아래 위치에 생성됩니다.

```text
bin/Debug/net8.0-windows/data/vault.dat
```

`data/` 폴더와 `vault.dat`는 `.gitignore`에 포함되어 있어 GitHub에 업로드되지 않습니다.

## 개발 환경

- C#
- .NET 8 이상
- WPF 데스크톱 앱
- .NET MAUI Android 모바일 앱
- 공통 암호화/저장 로직은 `AccountVault.Core`로 분리

## 실행 방법

필요한 SDK가 설치되어 있는지 확인합니다.

```powershell
dotnet --info
```

데스크톱 앱만 빌드합니다.

```powershell
dotnet build src/AccountVault.Desktop/AccountVault.Desktop.csproj
```

데스크톱 앱을 실행합니다.

```powershell
dotnet run --project src/AccountVault.Desktop/AccountVault.Desktop.csproj
```

## 모바일 앱

모바일 앱은 .NET MAUI 기반이며 현재 Android 타깃을 우선 지원합니다.

모바일 앱은 데스크톱과 같은 `AccountVault.Core`를 사용합니다.

- 같은 `AccountItem`, `VaultData`, `VaultFile` 모델 사용
- 같은 PBKDF2-SHA256 / AES-GCM 암호화 로직 사용
- 같은 `vault.dat` JSON 메타데이터 구조 사용

현재 모바일 앱에서 구현된 기능은 다음과 같습니다.

- 최초 마스터 비밀번호 설정
- 로그인
- 계정 목록 및 검색
- 계정 추가, 수정, 삭제
- 아이디 및 비밀번호 복사
- 비밀번호 복사 후 30초 뒤 조건부 클립보드 삭제
- 백업 내보내기
- 앱이 백그라운드로 전환될 때 세션 잠금

백업 가져오기는 모바일 파일 선택과 비밀번호 재확인 UI를 더 다듬은 뒤 추가할 예정입니다.

모바일 저장 위치는 데스크톱과 다릅니다. 모바일에서는 실행 파일 옆 경로를 사용할 수 없으므로 앱 전용 데이터 폴더 아래에 저장합니다.

```text
앱 전용 데이터 폴더/data/vault.dat
```

Android 빌드에 필요한 워크로드를 설치합니다.

```powershell
dotnet workload restore src/AccountVault.Mobile/AccountVault.Mobile.csproj
```

이 저장소에서는 Android SDK를 워크스페이스 내부 `.android-sdk` 폴더에 설치해 빌드할 수 있습니다.

```powershell
dotnet build src/AccountVault.Mobile/AccountVault.Mobile.csproj `
  -t:InstallAndroidDependencies `
  -f net8.0-android `
  -p:AndroidSdkDirectory=D:\AIStudio\AccountVault\.android-sdk `
  -p:AcceptAndroidSDKLicenses=True
```

모바일 앱을 빌드합니다.

```powershell
dotnet build src/AccountVault.Mobile/AccountVault.Mobile.csproj `
  -p:AndroidSdkDirectory=D:\AIStudio\AccountVault\.android-sdk
```

Android 환경까지 준비된 뒤에는 전체 솔루션을 한 번에 빌드할 수 있습니다.

```powershell
dotnet build AccountVault.sln
```

## 사용 흐름

1. 최초 실행 시 마스터 비밀번호를 설정합니다.
2. 빈 보관함이 생성되고 `data/vault.dat` 파일이 만들어집니다.
3. 이후 실행 시 마스터 비밀번호로 로그인합니다.
4. 메인 화면에서 계정을 추가, 수정, 삭제하거나 검색합니다.
5. 필요한 경우 `vault.dat` 파일을 백업으로 내보내거나 가져올 수 있습니다.

## 프로젝트 구조

```text
AccountVault
├─ AccountVault.sln
├─ src
│  ├─ AccountVault.Core
│  │  ├─ Models
│  │  │  ├─ AccountItem.cs
│  │  │  ├─ VaultData.cs
│  │  │  └─ VaultFile.cs
│  │  └─ Services
│  │     ├─ CryptoService.cs
│  │     ├─ VaultService.cs
│  │     ├─ VaultSession.cs
│  │     └─ VaultException.cs
│  ├─ AccountVault.Desktop
│  │  ├─ Services
│  │  ├─ ViewModels
│  │  ├─ Views
│  │  ├─ App.xaml
│  │  └─ AccountVault.Desktop.csproj
│  └─ AccountVault.Mobile
│     ├─ Pages
│     ├─ Services
│     ├─ Platforms
│     ├─ App.xaml
│     └─ AccountVault.Mobile.csproj
└─ README.md
```

## 주의사항

- 마스터 비밀번호를 잊으면 `vault.dat`를 복구할 수 없습니다.
- `vault.dat` 파일은 암호화되어 있지만, 안전한 위치에 백업하는 것을 권장합니다.
- 백업 가져오기는 복호화 테스트를 통과한 파일만 현재 보관함으로 교체합니다.
- 비밀번호는 클립보드에 복사된 뒤 30초 후 삭제되지만, 클립보드 기록 기능이 켜져 있는 환경에서는 별도 주의가 필요합니다.

## 라이선스

이 프로젝트는 [MIT 라이선스](LICENSE)로 배포됩니다.

저작권 및 라이선스 고지를 유지하면 상업적 이용, 수정 및 재배포가 가능합니다.
