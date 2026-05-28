# Equipment Program

C# WPF로 제작한 장비 배치 및 관리 프로그램입니다.  
연구실 또는 작업 공간의 기구를 지도 형태로 배치하고, 각 기구에 포함된 장비 정보를 등록, 검색, 관리할 수 있습니다.

<p align="center">
  <img src="docs/images/main-screen.png" alt="메인 화면" width="850">
</p>

## 주요 기능

- 테이블, 선반, 카트 배치
- 드래그 앤 드롭으로 기구 위치 지정
- 기구 이동, 크기 조절, 90도 회전, 삭제
- 장비 이름, 장비 구분, UL 번호, Global 번호, 비고 관리
- 장비 사진 등록 및 미리보기
- 여러 지도 생성, 이름 변경, 삭제
- 장비 이름, UL 번호, Global 번호 기준 검색
- 배치 정보와 장비 정보를 JSON 파일로 저장

## 배치 기능

좌측 팔레트에서 테이블, 선반, 카트를 맵으로 드래그해 원하는 위치에 배치할 수 있습니다.

<p align="center">
  <img src="docs/images/placement-1.png" alt="배치 화면 1" width="760">
</p>

배치된 기구는 이동, 크기 조절, 90도 회전, 삭제가 가능합니다.

<p align="center">
  <img src="docs/images/placement-2.png" alt="배치 화면 2" width="760">
</p>

## 편집 기능

편집 모드에서는 기구 이름과 팀 이름을 입력하고, 장비 추가 버튼으로 장비 목록을 관리할 수 있습니다.

<p align="center">
  <img src="docs/images/edit-1.png" alt="편집 화면 1" width="760">
</p>

장비별로 이름, 장비 구분, UL 번호, Global 번호, 사진, 비고를 입력할 수 있습니다.

<p align="center">
  <img src="docs/images/edit-2.png" alt="편집 화면 2" width="760">
</p>

사진 추가 기능을 통해 장비 이미지를 등록하고 미리볼 수 있습니다.

<p align="center">
  <img src="docs/images/edit-3.png" alt="편집 화면 3" width="760">
</p>

<p align="center">
  <img src="docs/images/edit-4.png" alt="편집 화면 4" width="760">
</p>

수정한 기구 이름과 팀 이름은 메인 화면에 바로 반영됩니다.

<p align="center">
  <img src="docs/images/edit-5.png" alt="편집 화면 5" width="760">
</p>

## 검색 기능

검색 창에서 장비 이름, UL 번호, Global 번호를 기준으로 장비를 찾을 수 있습니다.

<p align="center">
  <img src="docs/images/search-1.png" alt="검색 화면 1" width="760">
</p>

검색 결과에는 장비 이름, 소속 기구, 맵 이름, UL 번호, Global 번호가 표시됩니다.

<p align="center">
  <img src="docs/images/search-2.png" alt="검색 화면 2" width="760">
</p>

검색 결과를 선택하면 해당 장비가 있는 맵으로 이동하고, 장비가 포함된 기구가 강조 표시됩니다.

<p align="center">
  <img src="docs/images/search-3.png" alt="검색 화면 3" width="760">
</p>

기구를 선택하면 장비 위치를 더 쉽게 확인할 수 있습니다.

<p align="center">
  <img src="docs/images/search-4.png" alt="검색 화면 4" width="760">
</p>

## 개발 환경

- Windows
- .NET 10
- C#
- WPF
- Visual Studio

## 실행 방법

저장소를 클론합니다.

```bash
git clone https://github.com/jungwooyoung1234567/Equipment-program.git
```

프로젝트 폴더로 이동합니다.

```bash
cd Equipment-program
```

Visual Studio에서 `UL_project.csproj` 파일을 열고 실행합니다.

.NET CLI를 사용할 경우 다음 명령어로 실행할 수 있습니다.

```bash
dotnet run --project UL_project/UL_project.csproj
```

## 저장 데이터

프로그램 실행 폴더에 다음 파일이 생성됩니다.

```text
layout.json
layout.json.bak
```

- `layout.json`: 현재 지도, 기구 위치, 장비 정보 저장
- `layout.json.bak`: 기존 저장 파일 백업
