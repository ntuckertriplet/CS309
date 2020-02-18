package cs309.selectunes.activities

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import androidx.appcompat.app.AppCompatActivity
import com.android.volley.Request
import com.android.volley.Response
import com.android.volley.toolbox.JsonObjectRequest
import com.android.volley.toolbox.StringRequest
import com.android.volley.toolbox.Volley
import com.spotify.sdk.android.authentication.AuthenticationClient
import com.spotify.sdk.android.authentication.AuthenticationResponse
import com.spotify.sdk.android.authentication.LoginActivity.REQUEST_CODE
import cs309.selectunes.R
import cs309.selectunes.utils.HttpUtils
import cs309.selectunes.utils.SpotifyUtils
import java.nio.charset.StandardCharsets


class ChooseActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_choose)

        val createButton = findViewById<Button>(R.id.create_party_button)
        val joinButton = findViewById<Button>(R.id.join_party_button)

        createButton.setOnClickListener {
            SpotifyUtils.login(this)
        }

        joinButton.setOnClickListener {
        }
    }

    override fun onActivityResult(requestCode: Int, resultCode: Int, intent: Intent?) {
        super.onActivityResult(requestCode, resultCode, intent)
        if (requestCode == REQUEST_CODE) {
            val response = AuthenticationClient.getResponse(resultCode, intent)
            if (response.type == AuthenticationResponse.Type.CODE) {
                addSpotifyLogin(response.code)
                println("Spotify Response Code ${response.code}")
                createParty()
            } else if (response.type == AuthenticationResponse.Type.ERROR) {
                println("There was an error while logging into Spotify")
            }
        }
    }

    private fun createParty() {
        val stringRequest = JsonObjectRequest(Request.Method.GET, "https://coms-309-jr-2.cs.iastate.edu/api/Auth/oauthisdumb", null, Response.Listener {
            val joinCode = it.getString("joinCode")
            val hostIntent = Intent(this, HostMenuActivity::class.java)
            hostIntent.putExtra("code", joinCode)
            startActivity(hostIntent)
        }, Response.ErrorListener {
            println("There was an error with the create party response. Code: ${it.networkResponse.statusCode}")
        })
        val requestQueue = Volley.newRequestQueue(this, HttpUtils.createAuthCookie(this))
        requestQueue.add(stringRequest)
    }

    private fun addSpotifyLogin(auth: String) {
        val url = "https://coms-309-jr-2.cs.iastate.edu/api/Auth/Callback?Code=$auth"
        val jsonObjectRequest = object : StringRequest(Method.GET, url,
                Response.Listener {},
                Response.ErrorListener {
                    println("Error adding spotify login: ${it.networkResponse.statusCode}")
                    println(it.networkResponse.data.toString(StandardCharsets.UTF_8))
                }) {
            override fun getParams(): Map<String, String> {
                val params: MutableMap<String, String> = HashMap()
                params["Code"] = auth
                return params
            }
        }
        println(jsonObjectRequest.url)
        val requestQueue = Volley.newRequestQueue(this, HttpUtils.createAuthCookie(this))
        requestQueue.add(jsonObjectRequest)
    }
}
