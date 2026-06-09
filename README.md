# the-gatekeeper-api
# The Gatekeeper API

Một API có “cổng gác”: request thường được cho qua, request thiếu quyền bị chặn, request lỗi có requestId để truy vết, và mỗi lần push code thì GitHub Actions tự build/test/deploy lên AWS.

## Story

Developer push code  
→ GitHub Actions builds and tests  
→ Deploy to AWS  
→ Request goes through API Gateway  
→ Backend handles request  
→ Errors can be traced using requestId in CloudWatch

## Week Goal

Build a small API gateway demo with:

- Public endpoint
- Protected endpoint
- Version endpoint
- Intentional error endpoint
- GitHub Actions build/test/deploy
- AWS API Gateway
- CloudWatch logs
- Troubleshooting runbook

## Day 1 Evidence

- AWS CLI identity screenshot
- IAM/security note
- GitHub repo initialized