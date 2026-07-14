Login (POST)

http://surfscores.com/api/v1/users/login

{

  "email": "roberto.r3solucionesweb@gmail.com",

  "password": "Mikala79??"

}

Agree (POST)

http://surfscores.com/api/v1/users/agree

{

  "email":"roberto.r3solucionesweb@gmail.com",

  "password":"Mikala79??",

  "agree":"yes"

}

Con el token que me retorna el login, debo enviarlo a todos los endpoints.

Ejemplo:

POST events/organization

{

  "token": "888|oHiZ77D8VPFDKia0klcgHgZTzwNaEydRifhSqJWZ9d7c241c" ,

   "id": 13

}