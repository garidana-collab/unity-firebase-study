Write-Host "=== Git 이력 초기화 시작 ===" -ForegroundColor Cyan

# 현재 폴더 확인
$projectPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectPath
Write-Host "경로: $projectPath"

# 1. .git 폴더 삭제
Write-Host "`n[1/4] 기존 git 이력 삭제 중..." -ForegroundColor Yellow
Remove-Item -Recurse -Force ".git" -ErrorAction SilentlyContinue
Write-Host "완료" -ForegroundColor Green

# 2. git 새로 초기화
Write-Host "`n[2/4] git 초기화 중..." -ForegroundColor Yellow
git init
git branch -M main
Write-Host "완료" -ForegroundColor Green

# 3. 파일 추가 및 커밋 (secrets는 .gitignore로 제외됨)
Write-Host "`n[3/4] 파일 스테이징 및 커밋 중..." -ForegroundColor Yellow
git add .
git status --short
git commit -m "initial commit (API secrets removed from history)"
Write-Host "완료" -ForegroundColor Green

# 4. 원격 연결 및 강제 푸시
Write-Host "`n[4/4] GitHub에 강제 푸시 중..." -ForegroundColor Yellow
git remote add origin https://github.com/garidana-collab/unity-firebase-study.git
git push -f origin main
Write-Host "완료" -ForegroundColor Green

Write-Host "`n=== 완료! API 키 이력이 제거되었습니다 ===" -ForegroundColor Cyan
pause
