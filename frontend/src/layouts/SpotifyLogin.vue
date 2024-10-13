<template>
  <div>
    <h1>Login with Spotify</h1>
    <button @click="loginWithSpotify" class="login-button">
      <img src="@/assets/spotify-icon.png" alt="Spotify Icon" class="spotify-icon" />
      Login with Spotify
    </button>
  </div>
</template>

<script>
export default {
  name: 'SpotifyLogin',
  methods: {
    loginWithSpotify() {
      window.location.href = 'http://localhost:5000/auth/login';
    }
  }
};
</script>

<style>
.login-button {
  display: flex;
  align-items: center;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 25px;
  cursor: pointer;
  font-size: 16px;
}

.spotify-icon {
  width: 20px;
  height: 20px;
  margin-right: 10px;
}
</style>
