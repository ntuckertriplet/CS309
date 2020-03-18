from django.contrib.auth import logout
from django.http import HttpResponse
from django.shortcuts import render, get_object_or_404

from django.views import View

from djangorewrite.selectunesauth.forms import *
from djangorewrite.selectunesauth.models import RewriteUser


class DefaultView(View):
    def get(self):
        pass


class LoginView(View):
    def get(self):
        pass

    def post(self, request):
        pass


class LogoutView(View):
    def get(self):
        pass

    def post(self, request, *args, **kwargs):
        user = get_object_or_404(RewriteUser)
        RewriteUser.objects.filter(user.email).delete()
        logout(request)
        return HttpResponse("success")


class GetCurrentUserEmail(View):
    def get(self):
        user = get_object_or_404(RewriteUser)
        return user.email


class RegisterView(View):
    def get(self):
        pass

    def post(self, request):
        user = get_object_or_404(RewriteUser)
        form = AddRewriteUser(user)
        if form.is_valid():
            new_user = form.save(commit=False)
            new_user.save()
            return HttpResponse("success")

        return HttpResponse("bad data")


class BanView(View):
    def get(self):
        pass


class CallbackView(View):
    def get(self):
        pass
